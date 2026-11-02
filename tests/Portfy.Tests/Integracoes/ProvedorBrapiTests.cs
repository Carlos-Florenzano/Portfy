using System.Net;
using Microsoft.Extensions.Options;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;
using Portfy.Services.Integracoes;

namespace Portfy.Tests.Integracoes;

public class ProvedorBrapiTests
{
    // Payload real da BRAPI (campos usados pelo adaptador), verificado em 2026-11-01.
    private const string CorpoComCotacao = """
        {
          "results": [
            {
              "symbol": "PETR4",
              "shortName": "PETR4",
              "longName": "Petroleo Brasileiro SA Pfd",
              "currency": "BRL",
              "regularMarketPrice": 52.71,
              "regularMarketChange": 0.51,
              "regularMarketChangePercent": 0.98,
              "regularMarketTime": "2026-11-01T22:15:46.000Z"
            }
          ],
          "requestedAt": "2026-11-02T01:55:37.047Z",
          "took": 0
        }
        """;

    private const string CorpoDeErro = """
        {"error": true, "message": "Token de autenticação não fornecido", "code": "MISSING_TOKEN"}
        """;

    private static ProvedorBrapi CriarProvedor(ManipuladorHttpFalso manipulador, string token = "TOKEN-SECRETO") =>
        new(new HttpClient(manipulador) { BaseAddress = new Uri("https://brapi.dev") },
            Options.Create(new OpcoesDeIntegracao { TokenBrapi = token }),
            new RelogioFalso(Cenario.Instante));

    [Fact]
    public async Task ObterCotacao_MapeiaOPayloadParaOContratoPadronizado()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, CorpoComCotacao));
        ProvedorBrapi provedor = CriarProvedor(manipulador);

        CotacaoResponse? cotacao = await provedor.ObterCotacaoAsync("PETR4", CancellationToken.None);

        Assert.NotNull(cotacao);
        Assert.Equal("PETR4", cotacao!.Ticker);
        Assert.Equal("Petroleo Brasileiro SA Pfd", cotacao.Nome);
        Assert.Equal(52.71m, cotacao.Preco);
        Assert.Equal(0.98m, cotacao.VariacaoPercentual);
        Assert.Equal("BRL", cotacao.Moeda);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 22, 15, 46, TimeSpan.Zero), cotacao.AtualizadaEm);
        Assert.Equal("Brapi", cotacao.Fonte);
    }

    [Fact]
    public async Task ObterCotacao_ConsultaARotaDeCotacaoComOTicker()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, CorpoComCotacao));

        await CriarProvedor(manipulador).ObterCotacaoAsync("petr4", CancellationToken.None);

        Uri chamada = Assert.Single(manipulador.Requisicoes);
        Assert.Contains("/api/quote/petr4", chamada.AbsolutePath);
    }

    [Fact]
    public async Task ObterCotacao_TickerInexistenteDevolveNulo()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.NotFound, CorpoDeErro));

        Assert.Null(await CriarProvedor(manipulador).ObterCotacaoAsync("XPTO9", CancellationToken.None));
    }

    [Fact]
    public async Task ObterCotacao_TokenRecusadoViraIndisponibilidadeSemVazarCredencial()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.Unauthorized, CorpoDeErro));

        var excecao = await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => CriarProvedor(manipulador).ObterCotacaoAsync("PETR4", CancellationToken.None));

        Assert.DoesNotContain("TOKEN-SECRETO", excecao.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("brapi.dev", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObterCotacao_TimeoutViraIndisponibilidade()
    {
        var manipulador = ManipuladorHttpFalso.ComFalha(new TaskCanceledException("tempo esgotado"));

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => CriarProvedor(manipulador).ObterCotacaoAsync("PETR4", CancellationToken.None));
    }

    [Fact]
    public void ObterCotacao_SemTokenNaoEstaConfigurado()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, CorpoComCotacao));
        ProvedorBrapi provedor = CriarProvedor(manipulador, token: "   ");

        Assert.False(provedor.EstaConfigurado);
        Assert.Empty(manipulador.Requisicoes);
    }
}
