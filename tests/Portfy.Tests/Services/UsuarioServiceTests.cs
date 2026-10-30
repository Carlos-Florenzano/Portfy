using Portfy.Models;
using Portfy.Services;

namespace Portfy.Tests.Services;

public class UsuarioServiceTests
{
    private static UsuarioService CriarService() => new();

    [Fact]
    public void Cadastrar_GeraIdsSequenciais()
    {
        var service = CriarService();

        var primeiro = service.Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123");
        var segundo = service.Cadastrar("Bruno", 4000m, "bruno@portfy.test", "segredo456");

        Assert.Equal(1, primeiro.Id);
        Assert.Equal(2, segundo.Id);
        Assert.Equal(2, service.ObterTodos().Count);
    }

    [Fact]
    public void LoginJaExiste_DetectaLoginRepetidoIgnorandoCaixa()
    {
        var service = CriarService();
        service.Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123");

        Assert.True(service.LoginJaExiste("ANA@PORTFY.TEST"));
        Assert.False(service.LoginJaExiste("bruno@portfy.test"));
    }

    [Fact]
    public void Autenticar_DevolveUsuarioComASenhaCorreta()
    {
        var service = CriarService();
        service.Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123");

        var usuario = service.Autenticar("ana@portfy.test", "segredo123");

        Assert.NotNull(usuario);
        Assert.Equal("Ana", usuario!.Nome);
    }

    [Fact]
    public void Autenticar_RecusaSenhaIncorreta()
    {
        var service = CriarService();
        service.Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123");

        Assert.Null(service.Autenticar("ana@portfy.test", "errada"));
        Assert.Null(service.Autenticar("nao-existe@portfy.test", "segredo123"));
    }

    [Fact]
    public void Excluir_RemoveApenasOUsuarioInformado()
    {
        var service = CriarService();
        var ana = service.Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123");
        service.Cadastrar("Bruno", 4000m, "bruno@portfy.test", "segredo456");

        var removido = service.Excluir(ana.Id);

        Assert.NotNull(removido);
        Assert.Null(service.ObterPorId(ana.Id));
        Assert.Single(service.ObterTodos());
    }

    [Fact]
    public void AtualizarDados_RecusaNomeVazioSemAlterarOSalario()
    {
        var service = CriarService();
        var ana = service.Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123");

        Assert.Throws<ArgumentException>(() => service.AtualizarDados(ana.Id, " ", 5000m));
        Assert.Equal(3000m, service.ObterPorId(ana.Id)!.SalarioMensal);
    }

    [Fact]
    public void VerificarConta_DevolveNuloParaUsuarioInexistente()
    {
        Assert.Null(CriarService().VerificarConta(42));
    }
}
