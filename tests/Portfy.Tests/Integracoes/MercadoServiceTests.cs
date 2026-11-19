using Microsoft.Extensions.Caching.Memory;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;
using Portfy.Services.Integracoes;

namespace Portfy.Tests.Integracoes;

public class MercadoServiceTests
{
    private static ProvedorDeCotacaoFalso Brapi(
        decimal preco = 52.71m,
        Exception? falha = null,
        bool configurado = true) =>
        new(OrigemDeMercado.Brapi, _ => Cenario.Cotacao(OrigemDeMercado.Brapi, preco), falha, configurado);

    private static ProvedorDeCotacaoFalso Fmp(
        decimal preco = 150.5m,
        Exception? falha = null,
        bool configurado = true) =>
        new(OrigemDeMercado.Fmp, _ => Cenario.Cotacao(OrigemDeMercado.Fmp, preco), falha, configurado);

    [Fact]
    public async Task ObterCotacao_TickerDaB3PrefereABrapi()
    {
        var brapi = Brapi();
        var fmp = Fmp();
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        CotacaoResponse cotacao = await servico.ObterCotacaoAsync("PETR4", CancellationToken.None);

        Assert.Equal("Brapi", cotacao.Fonte);
        Assert.Equal(1, brapi.Chamadas);
        Assert.Equal(0, fmp.Chamadas);
    }

    [Fact]
    public async Task ObterCotacao_TickerGlobalPrefereAFmp()
    {
        var brapi = Brapi();
        var fmp = Fmp();
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        CotacaoResponse cotacao = await servico.ObterCotacaoAsync("AAPL", CancellationToken.None);

        Assert.Equal("Fmp", cotacao.Fonte);
        Assert.Equal(0, brapi.Chamadas);
        Assert.Equal(1, fmp.Chamadas);
    }

    [Fact]
    public async Task ObterCotacao_UsaAFmpQuandoABrapiFalha()
    {
        var brapi = Brapi(falha: new ServicoExternoIndisponivelException("fora do ar"));
        var fmp = Fmp();
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        CotacaoResponse cotacao = await servico.ObterCotacaoAsync("PETR4", CancellationToken.None);

        Assert.Equal("Fmp", cotacao.Fonte);
        Assert.Equal(1, brapi.Chamadas);
        Assert.Equal(1, fmp.Chamadas);
    }

    [Fact]
    public async Task ObterCotacao_UsaAFmpQuandoABrapiNaoEstaConfigurada()
    {
        var brapi = Brapi(configurado: false);
        var fmp = Fmp();
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        CotacaoResponse cotacao = await servico.ObterCotacaoAsync("PETR4", CancellationToken.None);

        Assert.Equal("Fmp", cotacao.Fonte);
        Assert.Equal(0, brapi.Chamadas);
    }

    [Fact]
    public async Task ObterCotacao_RespostaEmCacheNaoChamaOProvedorDeNovo()
    {
        var brapi = Brapi();
        var fmp = Fmp();
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        await servico.ObterCotacaoAsync("PETR4", CancellationToken.None);
        await servico.ObterCotacaoAsync("PETR4", CancellationToken.None);

        Assert.Equal(1, brapi.Chamadas);
    }

    [Fact]
    public async Task ObterCotacao_DisjuntorEvitaChamarDeNovoOProvedorQueFalhou()
    {
        var brapi = Brapi(falha: new ServicoExternoIndisponivelException("limite de requisições"));
        var fmp = Fmp();
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        await servico.ObterCotacaoAsync("PETR4", CancellationToken.None);
        await servico.ObterCotacaoAsync("VALE3", CancellationToken.None);

        Assert.Equal(1, brapi.Chamadas);
        Assert.Equal(2, fmp.Chamadas);
    }

    [Fact]
    public async Task ObterCotacao_TickerInexistenteEmTodasAsFontesVira404()
    {
        var brapi = new ProvedorDeCotacaoFalso(OrigemDeMercado.Brapi);
        var fmp = new ProvedorDeCotacaoFalso(OrigemDeMercado.Fmp);
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => servico.ObterCotacaoAsync("XPTO9", CancellationToken.None));
    }

    [Fact]
    public async Task ObterCotacao_SemFonteDisponivelViraIndisponibilidade()
    {
        var brapi = Brapi(configurado: false);
        var fmp = Fmp(falha: new ServicoExternoIndisponivelException("fora do ar"));
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => servico.ObterCotacaoAsync("PETR4", CancellationToken.None));
    }

    [Fact]
    public async Task ObterCotacao_FalhaDasDuasFontesViraIndisponibilidade()
    {
        var brapi = Brapi(falha: new ServicoExternoIndisponivelException("brapi fora"));
        var fmp = Fmp(falha: new ServicoExternoIndisponivelException("fmp fora"));
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, fmp });

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => servico.ObterCotacaoAsync("PETR4", CancellationToken.None));
    }

    [Fact]
    public async Task ObterCotacao_TickerEmBrancoViraRequisicaoInvalida()
    {
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { Brapi(), Fmp() });

        await Assert.ThrowsAsync<ArgumentException>(
            () => servico.ObterCotacaoAsync("   ", CancellationToken.None));
    }

    [Fact]
    public async Task ObterCotacao_EntregaOTickerNormalizadoAoProvedor()
    {
        string? recebido = null;
        var brapi = new ProvedorDeCotacaoFalso(
            OrigemDeMercado.Brapi,
            ticker => { recebido = ticker; return Cenario.Cotacao(OrigemDeMercado.Brapi, 10m); });
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, Fmp() });

        await servico.ObterCotacaoAsync(" petr4 ", CancellationToken.None);

        Assert.Equal("PETR4", recebido);
    }

    [Fact]
    public async Task ObterCotacao_UsaOCacheIsoladoPorTicker()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var brapi = Brapi();
        MercadoService servico = Cenario.Servico(new IProvedorDeCotacoes[] { brapi, Fmp() }, cache: cache);

        await servico.ObterCotacaoAsync("PETR4", CancellationToken.None);
        await servico.ObterCotacaoAsync("VALE3", CancellationToken.None);

        Assert.Equal(2, brapi.Chamadas);
    }

    [Fact]
    public async Task ObterFundamentos_SemNenhumaFonteConfiguradaViraIndisponibilidade()
    {
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            fundamentos: new IProvedorDeFundamentos[]
            {
                new ProvedorDeFundamentosFalso(configurado: false),
                new ProvedorDeFundamentosFalso(configurado: false, origem: OrigemDeMercado.Brapi),
            });

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => servico.ObterFundamentosAsync("AAPL", CancellationToken.None));
    }

    [Fact]
    public async Task ObterFundamentos_DevolveOContratoPadronizado()
    {
        var fundamentos = new ProvedorDeFundamentosFalso(_ => new FundamentoResponse
        {
            Ticker = "AAPL",
            Nome = "Apple Inc.",
            Setor = "Technology",
            Cobertura = CoberturaDeDados.Completa,
            Fonte = OrigemDeMercado.Fmp.ToString(),
        });
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            fundamentos: new IProvedorDeFundamentos[] { fundamentos });

        FundamentoResponse resposta = await servico.ObterFundamentosAsync("AAPL", CancellationToken.None);

        Assert.Equal("Technology", resposta.Setor);
        Assert.Equal("Fmp", resposta.Fonte);
        Assert.Equal(CoberturaDeDados.Completa, resposta.Cobertura);
    }

    [Fact]
    public async Task ObterIndicadores_PreservaAsSeriesQueResponderam()
    {
        var indicadores = new ProvedorDeIndicadoresFalso(indicador => indicador switch
        {
            IndicadorEconomico.Ipca or IndicadorEconomico.Selic => Cenario.Indicador(indicador),
            IndicadorEconomico.Cdi => null,
            _ => throw new ServicoExternoIndisponivelException("Banco Central fora do ar."),
        });
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            indicadores: indicadores);

        IndicadoresResponse resposta = await servico.ObterIndicadoresAsync(CancellationToken.None);

        Assert.Equal(2, resposta.Indicadores.Count);
        Assert.Equal(2, resposta.Avisos.Count);
    }

    [Fact]
    public async Task ObterIndicadores_SemNenhumaSerieViraIndisponibilidade()
    {
        var indicadores = new ProvedorDeIndicadoresFalso(
            falha: new ServicoExternoIndisponivelException("Banco Central fora do ar."));
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            indicadores: indicadores);

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => servico.ObterIndicadoresAsync(CancellationToken.None));
    }

    [Fact]
    public void Disjuntor_LiberaOProvedorDepoisDoTempoDeBloqueio()
    {
        var relogio = new RelogioFalso(Cenario.Instante);
        var disjuntor = Cenario.Disjuntor(relogio);

        disjuntor.Abrir(OrigemDeMercado.Brapi, TimeSpan.FromSeconds(60));

        Assert.False(disjuntor.EstaDisponivel(OrigemDeMercado.Brapi));

        relogio.Avancar(TimeSpan.FromSeconds(61));

        Assert.True(disjuntor.EstaDisponivel(OrigemDeMercado.Brapi));
    }

    [Fact]
    public async Task ObterFundamentos_UsaABrapiParcialQuandoAFmpFalha()
    {
        var fmp = new ProvedorDeFundamentosFalso(
            falha: new ServicoExternoIndisponivelException("sem chave"));
        var brapi = new ProvedorDeFundamentosFalso(
            _ => new FundamentoResponse
            {
                Ticker = "PETR4",
                Nome = "Petroleo Brasileiro SA Pfd",
                PrecoLucro = 5.09m,
                Cobertura = CoberturaDeDados.Parcial,
                Fonte = OrigemDeMercado.Brapi.ToString(),
            },
            origem: OrigemDeMercado.Brapi);
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            fundamentos: new IProvedorDeFundamentos[] { fmp, brapi });

        FundamentoResponse resposta = await servico.ObterFundamentosAsync("PETR4", CancellationToken.None);

        Assert.Equal("Brapi", resposta.Fonte);
        Assert.Equal(CoberturaDeDados.Parcial, resposta.Cobertura);
        Assert.Equal(5.09m, resposta.PrecoLucro);
    }

    [Fact]
    public async Task ObterFundamentos_UsaABrapiQuandoAFmpNaoCobreOTicker()
    {
        var fmp = new ProvedorDeFundamentosFalso();
        var brapi = new ProvedorDeFundamentosFalso(
            _ => new FundamentoResponse { Ticker = "HGLG11", Cobertura = CoberturaDeDados.Parcial },
            origem: OrigemDeMercado.Brapi);
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            fundamentos: new IProvedorDeFundamentos[] { fmp, brapi });

        FundamentoResponse resposta = await servico.ObterFundamentosAsync("HGLG11", CancellationToken.None);

        Assert.Equal(CoberturaDeDados.Parcial, resposta.Cobertura);
        Assert.Equal(1, fmp.Chamadas);
    }

    [Fact]
    public async Task ObterFundamentos_PrefereAFmpComCoberturaCompleta()
    {
        var fmp = new ProvedorDeFundamentosFalso(
            _ => new FundamentoResponse { Setor = "Technology", Cobertura = CoberturaDeDados.Completa });
        var brapi = new ProvedorDeFundamentosFalso(
            _ => new FundamentoResponse { Cobertura = CoberturaDeDados.Parcial },
            origem: OrigemDeMercado.Brapi);
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            fundamentos: new IProvedorDeFundamentos[] { brapi, fmp });

        FundamentoResponse resposta = await servico.ObterFundamentosAsync("AAPL", CancellationToken.None);

        Assert.Equal(CoberturaDeDados.Completa, resposta.Cobertura);
        Assert.Equal(0, brapi.Chamadas);
    }

    [Fact]
    public async Task ObterFundamentos_TickerInexistenteEmTodasAsFontesVira404()
    {
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            fundamentos: new IProvedorDeFundamentos[]
            {
                new ProvedorDeFundamentosFalso(),
                new ProvedorDeFundamentosFalso(origem: OrigemDeMercado.Brapi),
            });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => servico.ObterFundamentosAsync("XPTO9", CancellationToken.None));
    }

    [Fact]
    public async Task ObterFundamentos_FalhaDasDuasFontesViraIndisponibilidade()
    {
        MercadoService servico = Cenario.Servico(
            new IProvedorDeCotacoes[] { Brapi(), Fmp() },
            fundamentos: new IProvedorDeFundamentos[]
            {
                new ProvedorDeFundamentosFalso(falha: new ServicoExternoIndisponivelException("fmp fora")),
                new ProvedorDeFundamentosFalso(
                    falha: new ServicoExternoIndisponivelException("brapi fora"),
                    origem: OrigemDeMercado.Brapi),
            });

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => servico.ObterFundamentosAsync("PETR4", CancellationToken.None));
    }
}
