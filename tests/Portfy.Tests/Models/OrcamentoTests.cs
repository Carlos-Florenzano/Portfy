using Portfy.Models;

namespace Portfy.Tests.Models;

public class OrcamentoTests
{
    [Fact]
    public void Construtor_RecusaCategoriaVazia()
    {
        Assert.Throws<ArgumentException>(() => new Orcamento(" ", 100m, 10m));
    }

    [Fact]
    public void Construtor_RecusaLimiteNegativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Orcamento("Moradia", -1m, 10m));
    }

    [Fact]
    public void Construtor_RecusaPercentualNegativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Orcamento("Moradia", 100m, -1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AdicionarDespesa_RecusaValorNaoPositivo(decimal valor)
    {
        var orcamento = new Orcamento("Moradia", 100m, 10m);

        Assert.Throws<ArgumentOutOfRangeException>(() => orcamento.AdicionarDespesa(valor));
        Assert.Equal(0m, orcamento.ValorGastoAtual);
    }

    [Fact]
    public void AdicionarDespesa_AcumulaGastoEAtualizaSaldoRestante()
    {
        var orcamento = new Orcamento("Moradia", 1000m, 25m);

        orcamento.AdicionarDespesa(400m);
        orcamento.AdicionarDespesa(100m);

        Assert.Equal(500m, orcamento.ValorGastoAtual);
        Assert.Equal(500m, orcamento.ObterSaldoRestante());
        Assert.False(orcamento.ValidarEstouroOrcamento());
    }

    [Fact]
    public void ValidarEstouroOrcamento_FicaVerdadeiroAoUltrapassarOLimite()
    {
        var orcamento = new Orcamento("Moradia", 100m, 10m);

        orcamento.AdicionarDespesa(150m);

        Assert.True(orcamento.ValidarEstouroOrcamento());
        Assert.Equal(-50m, orcamento.ObterSaldoRestante());
    }
}
