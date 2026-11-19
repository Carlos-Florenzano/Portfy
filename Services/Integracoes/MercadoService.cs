using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;

namespace Portfy.Services.Integracoes;

/// Orquestrador das fontes de mercado (ADR-020). Aplica a divisão de
/// responsabilidades — B3 na BRAPI, global na FMP, macro no Banco Central —,
/// faz fallback quando a preferida falha, respeita o disjuntor e guarda o
/// resultado em cache para não consumir cota de plano gratuito à toa.
public sealed class MercadoService : IMercadoService
{
    // Padrão de ticker da B3: quatro letras e um ou dois dígitos (PETR4, HGLG11).
    // É a regra determinística que decide qual fonte tem prioridade.
    private static readonly Regex PadraoDeTickerDaB3 =
        new("^[A-Z]{4}[0-9]{1,2}$", RegexOptions.CultureInvariant);

    private readonly IReadOnlyList<IProvedorDeCotacoes> _provedoresDeCotacao;
    private readonly IReadOnlyList<IProvedorDeFundamentos> _provedoresDeFundamentos;
    private readonly IProvedorDeIndicadores _provedorDeIndicadores;
    private readonly IMemoryCache _cache;
    private readonly DisjuntorDeProvedores _disjuntor;
    private readonly OpcoesDeIntegracao _opcoes;
    private readonly ILogger<MercadoService> _logger;

    public MercadoService(
        IEnumerable<IProvedorDeCotacoes> provedoresDeCotacao,
        IEnumerable<IProvedorDeFundamentos> provedoresDeFundamentos,
        IProvedorDeIndicadores provedorDeIndicadores,
        IMemoryCache cache,
        DisjuntorDeProvedores disjuntor,
        IOptions<OpcoesDeIntegracao> opcoes,
        ILogger<MercadoService> logger)
    {
        ArgumentNullException.ThrowIfNull(provedoresDeCotacao);
        ArgumentNullException.ThrowIfNull(opcoes);

        _provedoresDeCotacao = provedoresDeCotacao.ToList();
        ArgumentNullException.ThrowIfNull(provedoresDeFundamentos);

        // Fundamentos: a FMP entrega o contrato completo; a BRAPI entra como
        // fallback declarado parcial quando a FMP não cobre o ticker ou está fora.
        _provedoresDeFundamentos = provedoresDeFundamentos
            .OrderBy(provedor => provedor.Origem == OrigemDeMercado.Fmp ? 0 : 1)
            .ToList();
        _provedorDeIndicadores = provedorDeIndicadores
            ?? throw new ArgumentNullException(nameof(provedorDeIndicadores));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _disjuntor = disjuntor ?? throw new ArgumentNullException(nameof(disjuntor));
        _opcoes = opcoes.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CotacaoResponse> ObterCotacaoAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        string normalizado = NormalizarTicker(ticker);
        string chave = $"mercado:cotacao:{normalizado}";

        if (_cache.TryGetValue(chave, out CotacaoResponse? emCache) && emCache is not null)
            return emCache;

        bool algumaFonteIndisponivel = false;

        foreach (IProvedorDeCotacoes provedor in OrdemDeProvedores(normalizado))
        {
            if (!provedor.EstaConfigurado || !_disjuntor.EstaDisponivel(provedor.Origem))
            {
                algumaFonteIndisponivel = true;
                continue;
            }

            (CotacaoResponse? cotacao, bool falhou) = await TentarCotacaoAsync(
                provedor, normalizado, cancellationToken);

            algumaFonteIndisponivel |= falhou;

            if (cotacao is null)
                continue;

            _cache.Set(chave, cotacao, TimeSpan.FromSeconds(_opcoes.SegundosDeCacheDeCotacao));
            return cotacao;
        }

        // Fonte fora do ar não é a mesma coisa que ticker inexistente: sem nenhuma
        // fonte disponível, o 404 seria uma afirmação falsa.
        if (algumaFonteIndisponivel)
            throw new ServicoExternoIndisponivelException(
                $"Nenhuma fonte de cotação disponível respondeu para '{normalizado}'.");

        throw new KeyNotFoundException(
            $"O ticker '{normalizado}' não foi encontrado em nenhuma fonte de cotação.");
    }

    public async Task<FundamentoResponse> ObterFundamentosAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        string normalizado = NormalizarTicker(ticker);
        string chave = $"mercado:fundamento:{normalizado}";

        if (_cache.TryGetValue(chave, out FundamentoResponse? emCache) && emCache is not null)
            return emCache;

        bool algumaFonteIndisponivel = false;

        foreach (IProvedorDeFundamentos provedor in _provedoresDeFundamentos)
        {
            if (!provedor.EstaConfigurado || !_disjuntor.EstaDisponivel(provedor.Origem))
            {
                algumaFonteIndisponivel = true;
                continue;
            }

            (FundamentoResponse? fundamento, bool falhou) = await TentarFundamentoAsync(
                provedor, normalizado, cancellationToken);

            algumaFonteIndisponivel |= falhou;

            if (fundamento is null)
                continue;

            _cache.Set(chave, fundamento, TimeSpan.FromSeconds(_opcoes.SegundosDeCacheDeFundamento));
            return fundamento;
        }

        if (algumaFonteIndisponivel)
            throw new ServicoExternoIndisponivelException(
                $"Nenhuma fonte de fundamentos disponível respondeu para '{normalizado}'.");

        throw new KeyNotFoundException(
            $"O ticker '{normalizado}' não foi encontrado em nenhuma fonte de fundamentos.");
    }

    public async Task<IndicadoresResponse> ObterIndicadoresAsync(CancellationToken cancellationToken)
    {
        var indicadores = new List<IndicadorResponse>();
        var avisos = new List<string>();

        foreach (IndicadorEconomico indicador in Enum.GetValues<IndicadorEconomico>())
        {
            string chave = $"mercado:indicador:{indicador}";

            if (_cache.TryGetValue(chave, out IndicadorResponse? emCache) && emCache is not null)
            {
                indicadores.Add(emCache);
                continue;
            }

            if (!await TentarIndicadorAsync(indicador, chave, indicadores, avisos, cancellationToken))
                break;
        }

        if (indicadores.Count == 0)
            throw new ServicoExternoIndisponivelException(
                "Nenhum indicador macroeconômico pôde ser obtido do Banco Central.");

        return new IndicadoresResponse { Indicadores = indicadores, Avisos = avisos };
    }

    private async Task<(CotacaoResponse? Cotacao, bool Falhou)> TentarCotacaoAsync(
        IProvedorDeCotacoes provedor,
        string ticker,
        CancellationToken cancellationToken)
    {
        try
        {
            CotacaoResponse? cotacao = await provedor.ObterCotacaoAsync(ticker, cancellationToken);
            _disjuntor.Fechar(provedor.Origem);
            return (cotacao, false);
        }
        catch (ServicoExternoIndisponivelException excecao)
        {
            AbrirDisjuntor(provedor.Origem, excecao, "cotação", ticker);
            return (null, true);
        }
    }

    private async Task<(FundamentoResponse? Fundamento, bool Falhou)> TentarFundamentoAsync(
        IProvedorDeFundamentos provedor,
        string ticker,
        CancellationToken cancellationToken)
    {
        try
        {
            FundamentoResponse? fundamento = await provedor
                .ObterFundamentosAsync(ticker, cancellationToken);

            _disjuntor.Fechar(provedor.Origem);
            return (fundamento, false);
        }
        catch (ServicoExternoIndisponivelException excecao)
        {
            AbrirDisjuntor(provedor.Origem, excecao, "fundamentos", ticker);
            return (null, true);
        }
    }

    private async Task<bool> TentarIndicadorAsync(
        IndicadorEconomico indicador,
        string chave,
        List<IndicadorResponse> indicadores,
        List<string> avisos,
        CancellationToken cancellationToken)
    {
        if (!_disjuntor.EstaDisponivel(_provedorDeIndicadores.Origem))
        {
            avisos.Add("A fonte oficial dos indicadores está bloqueada após uma falha.");
            return false;
        }

        try
        {
            IndicadorResponse? resposta = await _provedorDeIndicadores
                .ObterIndicadorAsync(indicador, cancellationToken);

            _disjuntor.Fechar(_provedorDeIndicadores.Origem);

            if (resposta is null)
            {
                avisos.Add($"O indicador {indicador} não tem valor publicado no momento.");
                return true;
            }

            _cache.Set(chave, resposta, TimeSpan.FromSeconds(_opcoes.SegundosDeCacheDeIndicador));
            indicadores.Add(resposta);
            return true;
        }
        catch (ServicoExternoIndisponivelException excecao)
        {
            AbrirDisjuntor(_provedorDeIndicadores.Origem, excecao, "indicadores", indicador.ToString());
            avisos.Add("A fonte oficial falhou ao responder; as séries já obtidas foram preservadas.");
            return false;
        }
    }

    private void AbrirDisjuntor(
        OrigemDeMercado origem,
        ServicoExternoIndisponivelException excecao,
        string recurso,
        string referencia)
    {
        _disjuntor.Abrir(origem, TimeSpan.FromSeconds(_opcoes.SegundosDeBloqueioDoProvedor));
        _logger.LogWarning(excecao, "Fonte {Origem} falhou em {Recurso} para {Referencia}.",
            origem, recurso, referencia);
    }

    private IReadOnlyList<IProvedorDeCotacoes> OrdemDeProvedores(string ticker)
    {
        OrigemDeMercado preferida = EhTickerDaB3(ticker)
            ? OrigemDeMercado.Brapi
            : OrigemDeMercado.Fmp;

        // OrderBy é estável: dentro de cada grupo vale a ordem de registro no DI.
        return _provedoresDeCotacao
            .OrderBy(provedor => provedor.Origem == preferida ? 0 : 1)
            .ToList();
    }

    private static bool EhTickerDaB3(string ticker) => PadraoDeTickerDaB3.IsMatch(ticker);

    private static string NormalizarTicker(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("O ticker não pode ser vazio.", nameof(ticker));

        string normalizado = ticker.Trim().ToUpperInvariant();

        if (normalizado.Length > 20)
            throw new ArgumentException("O ticker deve ter no máximo 20 caracteres.", nameof(ticker));

        return normalizado;
    }
}
