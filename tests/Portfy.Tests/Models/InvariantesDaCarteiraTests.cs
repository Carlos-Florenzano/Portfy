using Portfy.Models;
using Xunit;

namespace Portfy.Tests.Models;

// Prova, por comportamento observável, as invariantes que o domínio já garante.
// É a lista mínima pedida na avaliação: compra sem saldo, venda sem posição,
// aporte nulo, redução maior que a quantidade e preço médio após várias compras.
public class InvariantesDaCarteiraTests
{
    private static Acao CriarAcao(decimal preco = 10m) =>
        new("PETR4", "Petrobras PN", preco, dividendYieldAnual: 0m);

    private static CarteiraSimulada CriarCarteira(decimal saldoInicial = 1_000m) =>
        new(usuarioId: 1, nome: "Principal", saldoInicial: saldoInicial);

    [Fact]
    public void ComprarAtivo_RecusaCompraSemSaldoEPreservaEstado()
    {
        CarteiraSimulada carteira = CriarCarteira(saldoInicial: 100m);
        Acao acao = CriarAcao(preco: 10m);

        InvalidOperationException erro = Assert.Throws<InvalidOperationException>(
            () => carteira.ComprarAtivo(acao, quantidade: 11));

        Assert.Contains("Saldo insuficiente", erro.Message);
        Assert.Equal(100m, carteira.SaldoDisponivel);
        Assert.Empty(carteira.Posicoes);
    }

    [Fact]
    public void ComprarAtivo_RecusaQuantidadeNaoPositiva()
    {
        CarteiraSimulada carteira = CriarCarteira();
        Acao acao = CriarAcao();

        Assert.Throws<ArgumentException>(() => carteira.ComprarAtivo(acao, quantidade: 0));
        Assert.Throws<ArgumentException>(() => carteira.ComprarAtivo(acao, quantidade: -1));
        Assert.Equal(1_000m, carteira.SaldoDisponivel);
        Assert.Empty(carteira.Posicoes);
    }

    [Fact]
    public void ComprarAtivo_RecusaAtivoNulo()
    {
        CarteiraSimulada carteira = CriarCarteira();

        Assert.Throws<ArgumentNullException>(() => carteira.ComprarAtivo(null!, quantidade: 1));
    }

    [Fact]
    public void VenderAtivo_RecusaVendaSemPosicao()
    {
        CarteiraSimulada carteira = CriarCarteira();
        Acao acao = CriarAcao();

        Assert.Throws<InvalidOperationException>(() => carteira.VenderAtivo(acao, quantidade: 1));
        Assert.Equal(1_000m, carteira.SaldoDisponivel);
        Assert.Empty(carteira.Posicoes);
    }

    [Fact]
    public void VenderAtivo_RecusaVendaMaiorQueAPosicaoEPreservaQuantidade()
    {
        CarteiraSimulada carteira = CriarCarteira();
        Acao acao = CriarAcao(preco: 10m);
        carteira.ComprarAtivo(acao, quantidade: 5);

        Assert.Throws<InvalidOperationException>(() => carteira.VenderAtivo(acao, quantidade: 6));

        Posicao posicao = Assert.Single(carteira.Posicoes);
        Assert.Equal(5, posicao.Quantidade);
        Assert.Equal(950m, carteira.SaldoDisponivel);
    }

    [Fact]
    public void VenderAtivo_RemoveAPosicaoQuandoAQuantidadeZera()
    {
        CarteiraSimulada carteira = CriarCarteira();
        Acao acao = CriarAcao(preco: 10m);
        carteira.ComprarAtivo(acao, quantidade: 4);

        carteira.VenderAtivo(acao, quantidade: 4);

        Assert.Empty(carteira.Posicoes);
        Assert.Equal(1_000m, carteira.SaldoDisponivel);
    }

    [Fact]
    public void AdicionarAporte_RecusaAporteNulo()
    {
        CarteiraSimulada carteira = CriarCarteira();

        Assert.Throws<ArgumentNullException>(() => carteira.AdicionarAporte(null!));
        Assert.Equal(1_000m, carteira.SaldoDisponivel);
        Assert.Empty(carteira.Aportes);
    }

    [Fact]
    public void AdicionarAporte_SomaAoSaldoQuandoOrigemValida()
    {
        CarteiraSimulada carteira = CriarCarteira(saldoInicial: 500m);
        var aporte = new AporteSimulado(
            id: 0,
            valorAportado: 250m,
            origemSalarioId: 1,
            carteiraId: 7);

        carteira.AdicionarAporte(aporte);

        Assert.Equal(750m, carteira.SaldoDisponivel);
        Assert.Single(carteira.Aportes);
    }

    [Fact]
    public void ReduzirQuantidade_RecusaRemocaoMaiorQueADisponivel()
    {
        var posicao = new Posicao(CriarAcao(preco: 10m), quantidade: 3, precoMedio: 10m);

        Assert.Throws<InvalidOperationException>(() => posicao.ReduzirQuantidade(4));
        Assert.Equal(3, posicao.Quantidade);
    }

    [Fact]
    public void ComprarAtivo_CalculaPrecoMedioPonderadoAposMultiplasCompras()
    {
        CarteiraSimulada carteira = CriarCarteira(saldoInicial: 1_000m);
        Acao acao = CriarAcao(preco: 10m);

        carteira.ComprarAtivo(acao, quantidade: 10);

        // Segunda compra com preço diferente: (10*10 + 10*20) / 20 = 15.
        acao.AtualizarPreco(20m);
        carteira.ComprarAtivo(acao, quantidade: 10);

        Posicao posicao = Assert.Single(carteira.Posicoes);
        Assert.Equal(20, posicao.Quantidade);
        Assert.Equal(15m, posicao.PrecoMedio);
        Assert.Equal(700m, carteira.SaldoDisponivel);
    }

    [Fact]
    public void CalcularPatrimonioTotal_SomaSaldoEValorDeMercado()
    {
        CarteiraSimulada carteira = CriarCarteira(saldoInicial: 1_000m);
        Acao acao = CriarAcao(preco: 10m);
        carteira.ComprarAtivo(acao, quantidade: 10);

        acao.AtualizarPreco(12m);

        // Saldo 900 + 10 cotas a 12 = 1.020.
        Assert.Equal(1_020m, carteira.CalcularPatrimonioTotal());
    }

    [Fact]
    public void CalcularRendimento_UsaAImplementacaoDoSubtipoViaReferenciaBase()
    {
        // Mesma chamada, mesma quantidade, resultados diferentes: é o polimorfismo
        // (Ativo.CalcularRendimento abstrato → cada subtipo implementa o seu).
        Ativo acao = new Acao("PETR4", "Petrobras PN", precoInicial: 100m, dividendYieldAnual: 12m);
        Ativo fii = new FundoImobiliario("HGLG11", "CSHG Logística", precoInicial: 100m, rendimentoMensalPorCota: 1.5m);
        Ativo rendaFixa = new RendaFixa("TESOURO", "Tesouro Selic", precoInicial: 100m, taxaAnualContratada: 6m);
        Ativo cripto = new Cripto("BTC", "Bitcoin", precoInicial: 100m, taxaStakingAnual: 24m);
        Ativo etf = new Etf("BOVA11", "BOVA11", precoInicial: 100m, indiceReferencia: "IBOV", taxaAdministracaoAnual: 1.2m);

        Assert.Equal(10m, acao.CalcularRendimento(10, 2m));
        Assert.Equal(15m, fii.CalcularRendimento(10, 2m));
        Assert.Equal(5m, rendaFixa.CalcularRendimento(10, 2m));
        Assert.Equal(20m, cripto.CalcularRendimento(10, 2m));
        Assert.Equal(19m, etf.CalcularRendimento(10, 2m));
    }

    [Fact]
    public void CalcularRendimento_RecusaQuantidadeZeroSemQuebrar()
    {
        Ativo fii = new FundoImobiliario("HGLG11", "CSHG Logística", precoInicial: 100m, rendimentoMensalPorCota: 1.5m);

        Assert.Equal(0m, fii.CalcularRendimento(0, 0m));
    }
}
