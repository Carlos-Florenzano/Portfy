using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Portfy.Dtos.Ativo;

namespace Portfy.Tests.Api;

/// Borda HTTP de GET api/ativo/{ticker}/rendimento: o valor devolvido tem de ser o
/// mesmo que a implementação do subtipo calcula, sem o controller refazer a conta.
public class RendimentoApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente;

    public RendimentoApiTests(ApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task Rendimento_DeAcao_Responde200ComOValorDoSubtipo()
    {
        HttpResponseMessage resposta =
            await _cliente.GetAsync("/api/ativo/PETR4/rendimento?quantidade=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        RendimentoEstimadoResponse? corpo =
            await resposta.Content.ReadFromJsonAsync<RendimentoEstimadoResponse>();

        Assert.NotNull(corpo);
        Assert.Equal("PETR4", corpo!.Ticker);
        Assert.Equal("Acao", corpo.Tipo);
        Assert.Equal(10, corpo.Quantidade);
        Assert.Equal(3.85m, corpo.RendimentoMensalEstimado);
    }

    [Fact]
    public async Task Rendimento_DeFundoImobiliario_UsaOutraFormulaNoMesmoContrato()
    {
        HttpResponseMessage resposta =
            await _cliente.GetAsync("/api/ativo/HGLG11/rendimento?quantidade=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        RendimentoEstimadoResponse? corpo =
            await resposta.Content.ReadFromJsonAsync<RendimentoEstimadoResponse>();

        Assert.NotNull(corpo);
        Assert.Equal("Fii", corpo!.Tipo);
        Assert.Equal(11.00m, corpo.RendimentoMensalEstimado);
    }

    [Fact]
    public async Task Rendimento_ComQuantidadeZero_Responde400()
    {
        HttpResponseMessage resposta =
            await _cliente.GetAsync("/api/ativo/PETR4/rendimento?quantidade=0");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Rendimento_DeTickerInexistente_Responde404()
    {
        HttpResponseMessage resposta =
            await _cliente.GetAsync("/api/ativo/NAOEXISTE/rendimento?quantidade=1");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
