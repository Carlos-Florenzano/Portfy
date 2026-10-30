using Portfy.Models;

namespace Portfy.Tests.Models;

public class UsuarioTests
{
    private static Usuario CriarUsuario(decimal salarioMensal = 3000m) =>
        new(1, "Ana Souza", salarioMensal, DateTime.UtcNow, "ana@portfy.test", "segredo123");

    [Fact]
    public void Construtor_RecusaSalarioNegativo()
    {
        Assert.Throws<ArgumentException>(() => CriarUsuario(-1m));
    }

    [Fact]
    public void Construtor_RecusaLoginVazio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario(1, "Ana Souza", 3000m, DateTime.UtcNow, " ", "segredo123"));
    }

    [Fact]
    public void Construtor_RecusaSenhaVazia()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario(1, "Ana Souza", 3000m, DateTime.UtcNow, "ana@portfy.test", " "));
    }

    [Fact]
    public void Construtor_NaoArmazenaASenhaEmTextoPuro()
    {
        var usuario = CriarUsuario();

        Assert.NotEqual("segredo123", usuario.SenhaHash);
        Assert.True(usuario.VerificarSenha("segredo123"));
    }

    [Fact]
    public void Construtor_ComecaComContaNaoVerificada()
    {
        Assert.False(CriarUsuario().ContaVerificada);
    }

    [Fact]
    public void VerificarSenha_RecusaSenhaDiferente()
    {
        Assert.False(CriarUsuario().VerificarSenha("outra-senha"));
    }

    [Fact]
    public void AlterarSenha_RecusaSenhaAtualIncorreta()
    {
        var usuario = CriarUsuario();

        Assert.Throws<ArgumentException>(() => usuario.AlterarSenha("errada", "nova-senha"));
        Assert.True(usuario.VerificarSenha("segredo123"));
    }

    [Fact]
    public void AlterarSenha_RecusaNovaSenhaVazia()
    {
        var usuario = CriarUsuario();

        Assert.Throws<ArgumentException>(() => usuario.AlterarSenha("segredo123", " "));
    }

    [Fact]
    public void AlterarSenha_TrocaApenasComASenhaAtualCorreta()
    {
        var usuario = CriarUsuario();

        usuario.AlterarSenha("segredo123", "nova-senha");

        Assert.True(usuario.VerificarSenha("nova-senha"));
        Assert.False(usuario.VerificarSenha("segredo123"));
    }

    [Fact]
    public void AtualizarSalario_RecusaValorNegativo()
    {
        var usuario = CriarUsuario();

        Assert.Throws<ArgumentException>(() => usuario.AtualizarSalario(-1m));
        Assert.Equal(3000m, usuario.SalarioMensal);
    }

    [Fact]
    public void VerificarConta_MarcaAContaComoVerificada()
    {
        var usuario = CriarUsuario();

        usuario.VerificarConta();

        Assert.True(usuario.ContaVerificada);
    }
}
