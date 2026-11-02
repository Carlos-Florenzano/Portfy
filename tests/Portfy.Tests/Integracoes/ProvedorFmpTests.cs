using System.Net;
using Microsoft.Extensions.Options;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;
using Portfy.Services.Integracoes;

namespace Portfy.Tests.Integracoes;

public class ProvedorFmpTests
{
    // A FMP mantém duas gerações de payload; os dois formatos abaixo são os que o
    // adaptador precisa aceitar sem mudar o contrato do Portfy.
    private const string CotacaoV3 = """
        [{"symbol":"AAPL","name":"Apple Inc.","price":150.5,"changesPercentage":1.2,
          "marketCap":2500000000000,"pe":25.4,"eps":6.1}]
        """;

    private const string CotacaoStable = """
        [{"symbol":"AAPL","companyName":"Apple Inc.","price":"150.5","changePercentage":1.2}]
        """;

    private const string Perfil = """
        [{"symbol":"AAPL","companyName":"Apple Inc.","sector":"Technology",
          "industry":"Consumer Electronics","country":"US","currency":"USD","beta":1.2,
          "marketCap":2500000000000,"description":"Apple Inc. projeta produtos."}]
        """;

    private const string Erro = """{"Error Message":"Invalid API KEY."}""";

    private static ProvedorFmp CriarProvedor(ManipuladorHttpFalso manipulador, string chave = "CHAVE-SECRETA") =>
        new(new HttpClient(manipulador) { BaseAddress = new Uri("https://financialmodelingprep.com") },
            Options.Create(new OpcoesDeIntegracao { ChaveFmp = chave }),
            new RelogioFalso(Cenario.Instante));

    [Fact]
    public async Task ObterCotacao_MapeiaOPayloadDaFmp()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, CotacaoV3));

        CotacaoResponse? cotacao = await CriarProvedor(manipulador)
            .ObterCotacaoAsync("AAPL", CancellationToken.None);

        Assert.NotNull(cotacao);
        Assert.Equal("AAPL", cotacao!.Ticker);
        Assert.Equal("Apple Inc.", cotacao.Nome);
        Assert.Equal(150.5m, cotacao.Preco);
        Assert.Equal(1.2m, cotacao.VariacaoPercentual);
        Assert.Equal("Fmp", cotacao.Fonte);
    }

    [Fact]
    public async Task ObterCotacao_AceitaOsNomesDeCampoDaGeracaoStable()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, CotacaoStable));

        CotacaoResponse? cotacao = await CriarProvedor(manipulador)
            .ObterCotacaoAsync("AAPL", CancellationToken.None);

        Assert.NotNull(cotacao);
        Assert.Equal(150.5m, cotacao!.Preco);
        Assert.Equal(1.2m, cotacao.VariacaoPercentual);
    }

    [Fact]
    public async Task ObterCotacao_ListaVaziaDevolveNulo()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, "[]"));

        Assert.Null(await CriarProvedor(manipulador).ObterCotacaoAsync("XPTO", CancellationToken.None));
    }

    [Fact]
    public async Task ObterCotacao_ObjetoDeErroViraIndisponibilidade()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, Erro));

        var excecao = await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => CriarProvedor(manipulador).ObterCotacaoAsync("AAPL", CancellationToken.None));

        Assert.DoesNotContain("CHAVE-SECRETA", excecao.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ObterFundamentos_CombinaPerfilECotacao()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas(
            (HttpStatusCode.OK, Perfil),
            (HttpStatusCode.OK, CotacaoV3));

        FundamentoResponse? fundamento = await CriarProvedor(manipulador)
            .ObterFundamentosAsync("AAPL", CancellationToken.None);

        Assert.NotNull(fundamento);
        Assert.Equal("Apple Inc.", fundamento!.Nome);
        Assert.Equal("Technology", fundamento.Setor);
        Assert.Equal("Consumer Electronics", fundamento.Industria);
        Assert.Equal("US", fundamento.Pais);
        Assert.Equal("USD", fundamento.Moeda);
        Assert.Equal(2500000000000m, fundamento.CapitalizacaoDeMercado);
        Assert.Equal(25.4m, fundamento.PrecoLucro);
        Assert.Equal(6.1m, fundamento.LucroPorAcao);
        Assert.Equal(1.2m, fundamento.Beta);
        Assert.Equal("Fmp", fundamento.Fonte);
        Assert.Equal(2, manipulador.Requisicoes.Count);
    }

    [Fact]
    public async Task ObterFundamentos_TickerInexistenteDevolveNulo()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, "[]"));

        Assert.Null(await CriarProvedor(manipulador)
            .ObterFundamentosAsync("XPTO", CancellationToken.None));
        Assert.Single(manipulador.Requisicoes);
    }

    [Fact]
    public void ObterFundamentos_SemChaveNaoEstaConfigurado()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, Perfil));
        ProvedorFmp provedor = CriarProvedor(manipulador, chave: string.Empty);

        Assert.False(provedor.EstaConfigurado);
        Assert.Empty(manipulador.Requisicoes);
    }
}
