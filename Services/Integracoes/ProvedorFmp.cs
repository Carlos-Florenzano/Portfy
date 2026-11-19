using System.Text.Json;
using Microsoft.Extensions.Options;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;

namespace Portfy.Services.Integracoes;

/// Ativos globais e dados fundamentalistas via FMP. A chave viaja na query
/// string; nenhuma mensagem ou log reproduz a URL.
public sealed class ProvedorFmp : ProvedorHttpBase, IProvedorDeCotacoes, IProvedorDeFundamentos
{
    private readonly OpcoesDeIntegracao _opcoes;
    private readonly IRelogio _relogio;

    public ProvedorFmp(
        HttpClient cliente,
        IOptions<OpcoesDeIntegracao> opcoes,
        IRelogio relogio)
        : base(cliente)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        _opcoes = opcoes.Value;
        _relogio = relogio ?? throw new ArgumentNullException(nameof(relogio));
    }

    public OrigemDeMercado Origem => OrigemDeMercado.Fmp;

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_opcoes.ChaveFmp);

    public async Task<CotacaoResponse?> ObterCotacaoAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        JsonElement? cotacao = await ObterPrimeiroItemAsync("/stable/quote", ticker, cancellationToken);

        if (cotacao is null)
            return null;

        decimal? preco = LeitorJson.Numero(cotacao.Value, "price", "regularMarketPrice");

        if (preco is null || preco <= 0m)
            return null;

        return new CotacaoResponse
        {
            Ticker = LeitorJson.Texto(cotacao.Value, "symbol")?.ToUpperInvariant() ?? ticker,
            Nome = LeitorJson.Texto(cotacao.Value, "name", "companyName") ?? ticker,
            Preco = preco.Value,
            VariacaoPercentual = LeitorJson.Numero(
                cotacao.Value, "changesPercentage", "changePercentage"),
            Moeda = LeitorJson.Texto(cotacao.Value, "currency") ?? string.Empty,
            AtualizadaEm = _relogio.UtcNow,
            Fonte = Origem.ToString(),
        };
    }

    public async Task<FundamentoResponse?> ObterFundamentosAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        JsonElement? perfil = await ObterPrimeiroItemAsync("/stable/profile", ticker, cancellationToken);

        if (perfil is null)
            return null;

        JsonElement? cotacao = await ObterPrimeiroItemAsync("/stable/quote", ticker, cancellationToken);

        return new FundamentoResponse
        {
            Ticker = LeitorJson.Texto(perfil.Value, "symbol")?.ToUpperInvariant() ?? ticker,
            Nome = LeitorJson.Texto(perfil.Value, "companyName", "name") ?? ticker,
            Setor = LeitorJson.Texto(perfil.Value, "sector") ?? string.Empty,
            Industria = LeitorJson.Texto(perfil.Value, "industry") ?? string.Empty,
            Pais = LeitorJson.Texto(perfil.Value, "country") ?? string.Empty,
            Moeda = LeitorJson.Texto(perfil.Value, "currency") ?? string.Empty,
            Descricao = LeitorJson.Texto(perfil.Value, "description") ?? string.Empty,
            CapitalizacaoDeMercado = LeitorJson.Numero(perfil.Value, "marketCap")
                ?? ValorDaCotacao(cotacao, "marketCap"),
            PrecoLucro = ValorDaCotacao(cotacao, "pe", "peRatio", "peRatioTTM"),
            LucroPorAcao = ValorDaCotacao(cotacao, "eps", "epsTtm"),
            Beta = LeitorJson.Numero(perfil.Value, "beta"),
            Cobertura = CoberturaDeDados.Completa,
            Fonte = Origem.ToString(),
        };
    }

    /// Múltiplos vêm da cotação e são opcionais: sem eles o perfil continua útil.
    private static decimal? ValorDaCotacao(JsonElement? cotacao, params string[] nomes) =>
        cotacao is null ? null : LeitorJson.Numero(cotacao.Value, nomes);

    private async Task<JsonElement?> ObterPrimeiroItemAsync(
        string rota,
        string ticker,
        CancellationToken cancellationToken)
    {
        string caminho = string.Concat(
            rota, "?symbol=", Uri.EscapeDataString(ticker),
            "&apikey=", Uri.EscapeDataString(_opcoes.ChaveFmp));

        using JsonDocument? documento = await ObterDocumentoAsync(caminho, cancellationToken);

        if (documento is null)
            return null;

        JsonElement raiz = documento.RootElement;

        // Chave inválida ou plano sem acesso: a FMP responde um objeto de erro.
        if (raiz.ValueKind != JsonValueKind.Array)
            throw new ServicoExternoIndisponivelException(
                "A fonte de dados globais recusou a requisição (chave inválida ou plano sem acesso).");

        if (raiz.GetArrayLength() == 0)
            return null;

        // Clone: o JsonDocument é descartado no fim do método, e os valores lidos
        // pelos aliases precisam continuar válidos fora dele.
        return raiz[0].Clone();
    }
}
