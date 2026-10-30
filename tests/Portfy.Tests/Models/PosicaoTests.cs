using Portfy.Models;

namespace Portfy.Tests.Models;

public class PosicaoTests
{
    private static Acao CriarAcao(decimal preco = 10m) => new("PETR4", "Petrobras PN", preco, 6m);

    [Fact]
    public void Construtor_RecusaAtivoNulo()
    {
        Assert.Throws<ArgumentNullException>(() => new Posicao(null!, 1, 10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Construtor_RecusaQuantidadeNaoPositiva(int quantidade)
    {
        Assert.Throws<ArgumentException>(() => new Posicao(CriarAcao(), quantidade, 10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Construtor_RecusaPrecoMedioNaoPositivo(decimal precoMedio)
    {
        Assert.Throws<ArgumentException>(() => new Posicao(CriarAcao(), 1, precoMedio));
    }

    [Fact]
    public void ReduzirQuantidade_RecusaReducaoMaiorQueADisponivel()
    {
        var posicao = new Posicao(CriarAcao(), 5, 10m);

        var excecao = Assert.Throws<InvalidOperationException>(() => posicao.ReduzirQuantidade(6));

        Assert.Contains("Não é possível remover", excecao.Message);
        Assert.Equal(5, posicao.Quantidade);
    }

    [Fact]
    public void ReduzirQuantidade_ReduzSomenteOValorInformado()
    {
        var posicao = new Posicao(CriarAcao(), 5, 10m);

        posicao.ReduzirQuantidade(2);

        Assert.Equal(3, posicao.Quantidade);
    }
}
