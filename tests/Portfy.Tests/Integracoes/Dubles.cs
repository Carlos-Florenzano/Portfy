using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;
using Portfy.Services.Integracoes;

namespace Portfy.Tests.Integracoes;

/// Manipulador HTTP determinístico: enfileira respostas, registra as URLs
/// chamadas e nunca toca a rede.
internal sealed class ManipuladorHttpFalso : HttpMessageHandler
{
    private readonly Queue<Func<Task<HttpResponseMessage>>> _respostas = new();
    private readonly List<Uri> _requisicoes = new();

    private ManipuladorHttpFalso()
    {
    }

    public IReadOnlyList<Uri> Requisicoes => _requisicoes;

    public static ManipuladorHttpFalso ComRespostas(
        params (HttpStatusCode Status, string Corpo)[] respostas)
    {
        var manipulador = new ManipuladorHttpFalso();

        foreach ((HttpStatusCode status, string corpo) in respostas)
        {
            manipulador._respostas.Enqueue(() => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(corpo, Encoding.UTF8, "application/json"),
            }));
        }

        return manipulador;
    }

    public static ManipuladorHttpFalso ComFalha(Exception falha)
    {
        var manipulador = new ManipuladorHttpFalso();
        manipulador._respostas.Enqueue(() => Task.FromException<HttpResponseMessage>(falha));
        return manipulador;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requisicoes.Add(request.RequestUri ?? new Uri("urn:sem-endereco"));

        if (_respostas.Count == 0)
            return Task.FromException<HttpResponseMessage>(
                new InvalidOperationException("O dublê não configurou resposta para esta requisição."));

        return _respostas.Dequeue()();
    }
}

/// Relógio controlado pelo teste.
internal sealed class RelogioFalso : IRelogio
{
    public RelogioFalso(DateTimeOffset agora) => Agora = agora;

    public DateTimeOffset Agora { get; private set; }

    public DateTimeOffset UtcNow => Agora;

    public void Avancar(TimeSpan intervalo) => Agora = Agora.Add(intervalo);
}

internal sealed class ProvedorDeCotacaoFalso : IProvedorDeCotacoes
{
    private readonly Func<string, CotacaoResponse?> _resposta;
    private readonly Exception? _falha;

    public ProvedorDeCotacaoFalso(
        OrigemDeMercado origem,
        Func<string, CotacaoResponse?>? resposta = null,
        Exception? falha = null,
        bool configurado = true)
    {
        Origem = origem;
        _resposta = resposta ?? (_ => null);
        _falha = falha;
        EstaConfigurado = configurado;
    }

    public OrigemDeMercado Origem { get; }

    public bool EstaConfigurado { get; }

    public int Chamadas { get; private set; }

    public Task<CotacaoResponse?> ObterCotacaoAsync(string ticker, CancellationToken cancellationToken)
    {
        Chamadas++;

        return _falha is null
            ? Task.FromResult(_resposta(ticker))
            : Task.FromException<CotacaoResponse?>(_falha);
    }
}

internal sealed class ProvedorDeFundamentosFalso : IProvedorDeFundamentos
{
    private readonly Func<string, FundamentoResponse?> _resposta;
    private readonly Exception? _falha;

    public ProvedorDeFundamentosFalso(
        Func<string, FundamentoResponse?>? resposta = null,
        Exception? falha = null,
        bool configurado = true)
    {
        _resposta = resposta ?? (_ => null);
        _falha = falha;
        EstaConfigurado = configurado;
    }

    public OrigemDeMercado Origem => OrigemDeMercado.Fmp;

    public bool EstaConfigurado { get; }

    public int Chamadas { get; private set; }

    public Task<FundamentoResponse?> ObterFundamentosAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        Chamadas++;

        return _falha is null
            ? Task.FromResult(_resposta(ticker))
            : Task.FromException<FundamentoResponse?>(_falha);
    }
}

internal sealed class ProvedorDeIndicadoresFalso : IProvedorDeIndicadores
{
    private readonly Func<IndicadorEconomico, IndicadorResponse?> _resposta;
    private readonly Exception? _falha;

    public ProvedorDeIndicadoresFalso(
        Func<IndicadorEconomico, IndicadorResponse?>? resposta = null,
        Exception? falha = null)
    {
        _resposta = resposta ?? (_ => null);
        _falha = falha;
    }

    public OrigemDeMercado Origem => OrigemDeMercado.BcbSgs;

    public Task<IndicadorResponse?> ObterIndicadorAsync(
        IndicadorEconomico indicador,
        CancellationToken cancellationToken) =>
        _falha is null
            ? Task.FromResult(_resposta(indicador))
            : Task.FromException<IndicadorResponse?>(_falha);
}

/// Fábrica comum dos cenários: cache isolado, disjuntor com relógio falso e
/// opções com TTL curto para o teste não depender de tempo real.
internal static class Cenario
{
    internal static MercadoService Servico(
        IEnumerable<IProvedorDeCotacoes> cotacoes,
        IProvedorDeFundamentos? fundamentos = null,
        IProvedorDeIndicadores? indicadores = null,
        DisjuntorDeProvedores? disjuntor = null,
        IMemoryCache? cache = null) =>
        new(cotacoes,
            fundamentos ?? new ProvedorDeFundamentosFalso(),
            indicadores ?? new ProvedorDeIndicadoresFalso(),
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            disjuntor ?? new DisjuntorDeProvedores(new RelogioFalso(Instante)),
            Options.Create(new OpcoesDeIntegracao()),
            NullLogger<MercadoService>.Instance);

    internal static DisjuntorDeProvedores Disjuntor(RelogioFalso relogio) => new(relogio);

    internal static readonly DateTimeOffset Instante = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    internal static CotacaoResponse Cotacao(OrigemDeMercado origem, decimal preco) => new()
    {
        Ticker = "PETR4",
        Nome = "Ativo de teste",
        Preco = preco,
        VariacaoPercentual = 1m,
        Moeda = "BRL",
        AtualizadaEm = Instante,
        Fonte = origem.ToString(),
    };

    internal static IndicadorResponse Indicador(IndicadorEconomico indicador) => new()
    {
        Codigo = indicador.ToString().ToUpperInvariant(),
        Descricao = "Série de teste",
        Valor = 1m,
        Data = new DateOnly(2026, 11, 1),
        Unidade = "% no mês",
        SerieSgs = "433",
        Fonte = OrigemDeMercado.BcbSgs.ToString(),
    };
}
