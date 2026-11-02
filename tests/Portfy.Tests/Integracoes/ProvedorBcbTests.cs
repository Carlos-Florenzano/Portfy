using System.Net;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;
using Portfy.Services.Integracoes;

namespace Portfy.Tests.Integracoes;

public class ProvedorBcbTests
{
    // Formato real do SGS: data dd/MM/yyyy e valor decimal em texto.
    private const string CorpoDoIpca = """[{"data":"01/09/2026","valor":"0.82"}]""";

    private static ProvedorBcb CriarProvedor(ManipuladorHttpFalso manipulador) =>
        new(new HttpClient(manipulador) { BaseAddress = new Uri("https://api.bcb.gov.br") });

    [Fact]
    public async Task ObterIndicador_MapeiaOValorEOficialDoSgs()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, CorpoDoIpca));

        IndicadorResponse? indicador = await CriarProvedor(manipulador)
            .ObterIndicadorAsync(IndicadorEconomico.Ipca, CancellationToken.None);

        Assert.NotNull(indicador);
        Assert.Equal("IPCA", indicador!.Codigo);
        Assert.Equal(0.82m, indicador.Valor);
        Assert.Equal(new DateOnly(2026, 9, 1), indicador.Data);
        Assert.Equal("433", indicador.SerieSgs);
        Assert.Equal("% no mês", indicador.Unidade);
        Assert.Equal("BcbSgs", indicador.Fonte);
    }

    [Theory]
    [InlineData(IndicadorEconomico.Ipca, "433")]
    [InlineData(IndicadorEconomico.Selic, "432")]
    [InlineData(IndicadorEconomico.Cdi, "4389")]
    [InlineData(IndicadorEconomico.Dolar, "1")]
    public async Task ObterIndicador_ConsultaASerieCorretaDoSgs(
        IndicadorEconomico indicador,
        string serie)
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, CorpoDoIpca));

        await CriarProvedor(manipulador).ObterIndicadorAsync(indicador, CancellationToken.None);

        Uri chamada = Assert.Single(manipulador.Requisicoes);
        Assert.Contains($"/bcdata.sgs.{serie}/", chamada.AbsolutePath);
    }

    [Fact]
    public async Task ObterIndicador_SerieSemValorPublicadoDevolveNulo()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, "[]"));

        Assert.Null(await CriarProvedor(manipulador)
            .ObterIndicadorAsync(IndicadorEconomico.Ipca, CancellationToken.None));
    }

    [Fact]
    public async Task ObterIndicador_DataForaDoFormatoDevolveNulo()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas(
            (HttpStatusCode.OK, """[{"data":"2026-09-01","valor":"0.82"}]"""));

        Assert.Null(await CriarProvedor(manipulador)
            .ObterIndicadorAsync(IndicadorEconomico.Ipca, CancellationToken.None));
    }

    [Fact]
    public async Task ObterIndicador_FalhaDoBancoCentralViraIndisponibilidade()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.InternalServerError, ""));

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => CriarProvedor(manipulador)
                .ObterIndicadorAsync(IndicadorEconomico.Selic, CancellationToken.None));
    }

    [Fact]
    public async Task ObterIndicador_FormatoInesperadoViraIndisponibilidade()
    {
        var manipulador = ManipuladorHttpFalso.ComRespostas((HttpStatusCode.OK, """{"erro":"x"}"""));

        await Assert.ThrowsAsync<ServicoExternoIndisponivelException>(
            () => CriarProvedor(manipulador)
                .ObterIndicadorAsync(IndicadorEconomico.Cdi, CancellationToken.None));
    }
}
