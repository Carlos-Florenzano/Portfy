using Portfy.Models;

namespace Portfy.Tests.Models;

public class OrcamentoTests
{
    [Fact]
    public void Construtor_RecusaUsuarioInvalido()
    {
        Assert.Throws<ArgumentException>(() => new Orcamento(0, "Moradia", 100m, 10m));
    }

    [Fact]
    public void Construtor_RecusaCategoriaVazia()
    {
        Assert.Throws<ArgumentException>(() => new Orcamento(1, " ", 100m, 10m));
    }

    [Fact]
    public void Construtor_RecusaLimiteNegativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Orcamento(1, "Moradia", -1m, 10m));
    }

    [Fact]
    public void Construtor_RecusaPercentualNegativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Orcamento(1, "Moradia", 100m, -1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AdicionarDespesa_RecusaValorNaoPositivo(decimal valor)
    {
        var orcamento = new Orcamento(1, "Moradia", 100m, 10m);

        Assert.Throws<ArgumentOutOfRangeException>(() => orcamento.AdicionarDespesa(valor));
        Assert.Equal(0m, orcamento.ValorGastoAtual);
    }

    [Fact]
    public void AdicionarDespesa_AcumulaGastoEAtualizaSaldoRestante()
    {
        var orcamento = new Orcamento(1, "Moradia", 1000m, 25m);

        orcamento.AdicionarDespesa(400m);
        orcamento.AdicionarDespesa(100m);

        Assert.Equal(500m, orcamento.ValorGastoAtual);
        Assert.Equal(500m, orcamento.ObterSaldoRestante());
        Assert.False(orcamento.ValidarEstouroOrcamento());
    }

    [Fact]
    public void ValidarEstouroOrcamento_FicaVerdadeiroAoUltrapassarOLimite()
    {
        var orcamento = new Orcamento(1, "Moradia", 100m, 10m);

        orcamento.AdicionarDespesa(150m);

        Assert.True(orcamento.ValidarEstouroOrcamento());
        Assert.Equal(-50m, orcamento.ObterSaldoRestante());
    }

    [Fact]
    public void Renomear_TrocaONomeSemTocarNoHistoricoDeGastos()
    {
        var orcamento = new Orcamento(1, "Moradia", 1000m, 25m);
        orcamento.AdicionarDespesa(200m);

        orcamento.Renomear("Casa");

        Assert.Equal("Casa", orcamento.NomeCategoria);
        Assert.Equal(200m, orcamento.ValorGastoAtual);
        Assert.Equal(1000m, orcamento.LimiteDefinido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Renomear_RecusaNomeVazio(string nome)
    {
        var orcamento = new Orcamento(1, "Moradia", 1000m, 25m);

        Assert.Throws<ArgumentException>(() => orcamento.Renomear(nome));
        Assert.Equal("Moradia", orcamento.NomeCategoria);
    }
}
