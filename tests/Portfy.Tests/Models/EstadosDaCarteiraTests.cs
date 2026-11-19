using Portfy.Models;

namespace Portfy.Tests.Models;

/// Ciclo de vida da carteira: três estados nomeados com transições permitidas e
/// proibidas. Cada teste falha se o agregado deixar de proteger o próprio estado
/// ou se uma transição recusada alterar saldo/posições.
public class EstadosDaCarteiraTests
{
    private static Acao CriarAcao(decimal preco) => new("PETR4", "Petrobras PN", preco, 6m);

    private static CarteiraSimulada CriarCarteira(decimal saldo = 1000m) =>
        new(usuarioId: 7, nome: "Principal", saldoInicial: saldo);

    [Fact]
    public void CarteiraNova_ComecaAtiva()
    {
        Assert.Equal(StatusCarteira.Ativa, CriarCarteira().Status);
    }

    [Fact]
    public void Suspender_DeAtivaVaiParaSuspensa_PreservandoIdentidade()
    {
        var carteira = CriarCarteira();
        int usuarioAntes = carteira.UsuarioId;
        string nomeAntes = carteira.Nome;
        decimal saldoAntes = carteira.SaldoDisponivel;

        carteira.Suspender();

        Assert.Equal(StatusCarteira.Suspensa, carteira.Status);
        // Identidade preservada: mesmo objeto, mesmo dono, mesmo nome, mesmo saldo.
        Assert.Equal(usuarioAntes, carteira.UsuarioId);
        Assert.Equal(nomeAntes, carteira.Nome);
        Assert.Equal(saldoAntes, carteira.SaldoDisponivel);
    }

    [Fact]
    public void Reativar_DeSuspensaVoltaParaAtiva()
    {
        var carteira = CriarCarteira();
        carteira.Suspender();

        carteira.Reativar();

        Assert.Equal(StatusCarteira.Ativa, carteira.Status);
    }

    [Fact]
    public void Encerrar_DeAtivaVaiParaEncerrada()
    {
        var carteira = CriarCarteira();

        carteira.Encerrar();

        Assert.Equal(StatusCarteira.Encerrada, carteira.Status);
    }

    [Fact]
    public void Encerrar_DeSuspensaVaiParaEncerrada()
    {
        var carteira = CriarCarteira();
        carteira.Suspender();

        carteira.Encerrar();

        Assert.Equal(StatusCarteira.Encerrada, carteira.Status);
    }

    [Fact]
    public void Reativar_DeEncerrada_EProibido_EMantemEstadoESaldo()
    {
        var carteira = CriarCarteira(500m);
        carteira.Encerrar();
        decimal saldoAntes = carteira.SaldoDisponivel;

        var excecao = Assert.Throws<InvalidOperationException>(() => carteira.Reativar());

        Assert.Contains("Encerrada", excecao.Message);
        Assert.Equal(StatusCarteira.Encerrada, carteira.Status);
        Assert.Equal(saldoAntes, carteira.SaldoDisponivel);
    }

    [Fact]
    public void Suspender_DeEncerrada_EProibido_EMantemEstado()
    {
        var carteira = CriarCarteira();
        carteira.Encerrar();

        Assert.Throws<InvalidOperationException>(() => carteira.Suspender());

        Assert.Equal(StatusCarteira.Encerrada, carteira.Status);
    }

    [Fact]
    public void Suspender_DeSuspensa_EProibido()
    {
        var carteira = CriarCarteira();
        carteira.Suspender();

        Assert.Throws<InvalidOperationException>(() => carteira.Suspender());

        Assert.Equal(StatusCarteira.Suspensa, carteira.Status);
    }

    [Fact]
    public void Encerrar_DeEncerrada_EProibido()
    {
        var carteira = CriarCarteira();
        carteira.Encerrar();

        Assert.Throws<InvalidOperationException>(() => carteira.Encerrar());

        Assert.Equal(StatusCarteira.Encerrada, carteira.Status);
    }

    [Fact]
    public void CarteiraSuspensa_RecusaCompraVendaEAporte_SemAlterarSaldoNemPosicoes()
    {
        var carteira = CriarCarteira(1000m);
        var ativo = CriarAcao(10m);
        var aporte = new AporteSimulado(1, 50m, origemSalarioId: 1, carteiraId: 1);

        // Compra válida antes de suspender: a posição existe de fato.
        carteira.ComprarAtivo(ativo, 5);
        decimal saldoAntes = carteira.SaldoDisponivel;
        int posicoesAntes = carteira.Posicoes.Count;

        carteira.Suspender();

        Assert.Throws<InvalidOperationException>(() => carteira.ComprarAtivo(ativo, 1));
        Assert.Throws<InvalidOperationException>(() => carteira.VenderAtivo(ativo, 1));
        Assert.Throws<InvalidOperationException>(() => carteira.AdicionarAporte(aporte));

        Assert.Equal(StatusCarteira.Suspensa, carteira.Status);
        Assert.Equal(saldoAntes, carteira.SaldoDisponivel);
        Assert.Equal(posicoesAntes, carteira.Posicoes.Count);
    }

    [Fact]
    public void CarteiraEncerrada_RecusaMovimentacao()
    {
        var carteira = CriarCarteira(1000m);
        var ativo = CriarAcao(10m);
        carteira.Encerrar();

        Assert.Throws<InvalidOperationException>(() => carteira.ComprarAtivo(ativo, 1));
        Assert.Throws<InvalidOperationException>(() => carteira.VenderAtivo(ativo, 1));

        Assert.Equal(1000m, carteira.SaldoDisponivel);
        Assert.Empty(carteira.Posicoes);
    }
}
