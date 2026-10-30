using Portfy.Models;
using Portfy.Services;
using Portfy.Services.Contracts;

namespace Portfy.Tests.Services;

public class AtivoServiceTests
{
    /// Dublê determinístico de IGeradorDeVariacao: remove a aleatoriedade do teste.
    private sealed class GeradorFixo : IGeradorDeVariacao
    {
        private readonly decimal _percentual;

        public GeradorFixo(decimal percentual) => _percentual = percentual;

        public decimal SortearPercentual(decimal percentualMinimo, decimal percentualMaximo) => _percentual;
    }

    [Fact]
    public void SimularVariacao_AplicaOPercentualSorteadoQuandoNaoInformado()
    {
        var service = new AtivoService(new GeradorFixo(10m));

        VariacaoSimulada? variacao = service.SimularVariacao("PETR4", null);

        Assert.NotNull(variacao);
        Assert.Equal(38.50m, variacao!.PrecoAnterior);
        Assert.Equal(42.35m, variacao.Ativo.PrecoAtual);
        Assert.Equal(10m, variacao.Ativo.RentabilidadeAcumulada);
    }

    [Fact]
    public void SimularVariacao_AplicaOPercentualCustomizadoQuandoInformado()
    {
        var service = new AtivoService(new GeradorFixo(50m));

        VariacaoSimulada? variacao = service.SimularVariacao("PETR4", -10m);

        Assert.NotNull(variacao);
        Assert.Equal(34.65m, variacao!.Ativo.PrecoAtual);
        Assert.Equal(-10m, variacao.Ativo.RentabilidadeAcumulada);
    }

    [Fact]
    public void SimularVariacao_RecusaTickerInexistente()
    {
        var service = new AtivoService(new GeradorFixo(10m));

        Assert.Null(service.SimularVariacao("XPTO", 5m));
    }

    [Fact]
    public void ObterPorTicker_IgnoraCaixaDoTicker()
    {
        var service = new AtivoService(new GeradorFixo(10m));

        Assert.NotNull(service.ObterPorTicker("petr4"));
    }

    [Fact]
    public void ObterTodos_DevolveAsSubclassesConcretasDoCatalogo()
    {
        var service = new AtivoService(new GeradorFixo(10m));

        var ativos = service.ObterTodos();

        Assert.Contains(ativos, a => a is Acao);
        Assert.Contains(ativos, a => a is FundoImobiliario);
        Assert.Contains(ativos, a => a is Cripto);
    }
}
