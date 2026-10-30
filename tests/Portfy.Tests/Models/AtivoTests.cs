using Portfy.Models;

namespace Portfy.Tests.Models;

public class AtivoTests
{
    [Fact]
    public void Construtor_RecusaTickerVazio()
    {
        Assert.Throws<ArgumentException>(() => new Acao(" ", "Petrobras", 10m, 6m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_RecusaPrecoInicialNaoPositivo(decimal precoInicial)
    {
        Assert.Throws<ArgumentException>(() => new Acao("PETR4", "Petrobras", precoInicial, 6m));
    }

    [Fact]
    public void Construtor_NormalizaOTickerParaMaiusculas()
    {
        Assert.Equal("PETR4", new Acao("petr4", "Petrobras", 10m, 6m).Ticker);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AtualizarPreco_RecusaPrecoNaoPositivo(decimal novoPreco)
    {
        var ativo = new Acao("PETR4", "Petrobras", 10m, 6m);

        Assert.Throws<ArgumentException>(() => ativo.AtualizarPreco(novoPreco));
        Assert.Equal(10m, ativo.PrecoAtual);
    }

    [Fact]
    public void AtualizarPreco_RecalculaARentabilidadeAcumulada()
    {
        var ativo = new Acao("PETR4", "Petrobras", 100m, 6m);

        ativo.AtualizarPreco(110m);

        Assert.Equal(110m, ativo.PrecoAtual);
        Assert.Equal(10m, ativo.RentabilidadeAcumulada);
    }

    // Polimorfismo (AV1, dimensão G): cada subtipo calcula o rendimento do seu jeito.
    [Fact]
    public void CalcularRendimento_UsaARegraDeCadaSubtipo()
    {
        Ativo acao = new Acao("PETR4", "Petrobras", 100m, 12m);
        Ativo fundo = new FundoImobiliario("HGLG11", "CGHG Logística", 100m, 1.10m);
        Ativo cripto = new Cripto("BTC", "Bitcoin", 100m, 12m);

        Assert.Equal(10m, acao.CalcularRendimento(10, 0m));
        Assert.Equal(11m, fundo.CalcularRendimento(10, 0m));
        Assert.Equal(10m, cripto.CalcularRendimento(10, 0m));
    }

    [Fact]
    public void CalcularRendimento_DevolveZeroSemQuantidade()
    {
        Ativo acao = new Acao("PETR4", "Petrobras", 100m, 12m);

        Assert.Equal(0m, acao.CalcularRendimento(0, 0m));
    }
}
