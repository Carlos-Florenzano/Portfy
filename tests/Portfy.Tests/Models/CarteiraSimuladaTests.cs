using Portfy.Models;

namespace Portfy.Tests.Models;

/// Invariantes de capital da carteira (AV1, dimensão C/H): cada teste falha se a regra
/// deixar de ser garantida pelo domínio.
public class CarteiraSimuladaTests
{
    private static Acao CriarAcao(decimal preco) => new("PETR4", "Petrobras PN", preco, 6m);

    [Fact]
    public void Construtor_RecusaSaldoInicialNegativo()
    {
        var excecao = Assert.Throws<ArgumentException>(() => new CarteiraSimulada(-1m));

        Assert.Equal("saldoInicial", excecao.ParamName);
    }

    [Fact]
    public void ComprarAtivo_RecusaCompraSemSaldo()
    {
        var carteira = new CarteiraSimulada(100m);
        var ativo = CriarAcao(50m);

        var excecao = Assert.Throws<InvalidOperationException>(() => carteira.ComprarAtivo(ativo, 3));

        Assert.Contains("Saldo insuficiente", excecao.Message);
        Assert.Empty(carteira.Posicoes);
    }

    [Fact]
    public void ComprarAtivo_RecusaAtivoNulo()
    {
        var carteira = new CarteiraSimulada(100m);

        Assert.Throws<ArgumentNullException>(() => carteira.ComprarAtivo(null!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ComprarAtivo_RecusaQuantidadeNaoPositiva(int quantidade)
    {
        var carteira = new CarteiraSimulada(100m);
        var ativo = CriarAcao(10m);

        Assert.Throws<ArgumentException>(() => carteira.ComprarAtivo(ativo, quantidade));
    }

    [Fact]
    public void ComprarAtivo_CalculaPrecoMedioAposMultiplasCompras()
    {
        var carteira = new CarteiraSimulada(1000m);
        var ativo = CriarAcao(10m);

        carteira.ComprarAtivo(ativo, 10);
        ativo.AtualizarPreco(20m);
        carteira.ComprarAtivo(ativo, 10);

        var posicao = Assert.Single(carteira.Posicoes);
        Assert.Equal(20, posicao.Quantidade);
        Assert.Equal(15m, posicao.PrecoMedio);
        Assert.Equal(700m, carteira.SaldoDisponivel);
    }

    [Fact]
    public void VenderAtivo_RecusaVendaSemPosicao()
    {
        var carteira = new CarteiraSimulada(100m);

        Assert.Throws<InvalidOperationException>(() => carteira.VenderAtivo(CriarAcao(10m), 1));
    }

    [Fact]
    public void VenderAtivo_RecusaVendaMaiorQueAPosicao()
    {
        var carteira = new CarteiraSimulada(100m);
        var ativo = CriarAcao(10m);
        carteira.ComprarAtivo(ativo, 2);

        Assert.Throws<InvalidOperationException>(() => carteira.VenderAtivo(ativo, 3));
        Assert.Equal(2, Assert.Single(carteira.Posicoes).Quantidade);
    }

    [Fact]
    public void VenderAtivo_RemoveAPosicaoQuandoAZera()
    {
        var carteira = new CarteiraSimulada(100m);
        var ativo = CriarAcao(10m);
        carteira.ComprarAtivo(ativo, 2);

        carteira.VenderAtivo(ativo, 2);

        Assert.Empty(carteira.Posicoes);
        Assert.Equal(100m, carteira.SaldoDisponivel);
    }

    [Fact]
    public void AdicionarAporte_RecusaAporteNulo()
    {
        var carteira = new CarteiraSimulada(100m);

        Assert.Throws<ArgumentNullException>(() => carteira.AdicionarAporte(null!));
    }

    [Fact]
    public void AdicionarAporte_SomaAoSaldoEDepoisAoPatrimonio()
    {
        var carteira = new CarteiraSimulada(0m);
        carteira.AdicionarAporte(new AporteSimulado(1, 250m, 1));

        Assert.Equal(250m, carteira.SaldoDisponivel);
        Assert.Single(carteira.Aportes);
        Assert.Equal(250m, carteira.CalcularPatrimonioTotal());
    }

    [Fact]
    public void CalcularPatrimonioTotal_SomaSaldoEValorDeMercado()
    {
        var carteira = new CarteiraSimulada(100m);
        var ativo = CriarAcao(10m);
        carteira.ComprarAtivo(ativo, 2);

        ativo.AtualizarPreco(15m);

        Assert.Equal(110m, carteira.CalcularPatrimonioTotal());
    }
}
