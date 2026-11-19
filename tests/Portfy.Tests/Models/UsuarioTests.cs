using Portfy.Models;
using Portfy.Services;

namespace Portfy.Tests.Models;

public class UsuarioTests
{
    // Um único hash BCrypt por execução: o custo de trabalho é proposital e
    // recalcular em cada teste deixaria a suíte lenta sem ganho de cobertura.
    private static readonly string HashDaSenha = new SenhaHasher().CriarHash("segredo123");

    private static Usuario CriarUsuario(decimal salarioMensal = 3000m) =>
        new(1, "Ana Souza", salarioMensal, DateTime.UtcNow, "ana@portfy.test", HashDaSenha);

    [Fact]
    public void Construtor_RecusaSalarioNegativo()
    {
        Assert.Throws<ArgumentException>(() => CriarUsuario(-1m));
    }

    [Fact]
    public void Construtor_RecusaLoginVazio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario(1, "Ana Souza", 3000m, DateTime.UtcNow, " ", HashDaSenha));
    }

    [Fact]
    public void Construtor_RecusaHashVazio()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario(1, "Ana Souza", 3000m, DateTime.UtcNow, "ana@portfy.test", " "));
    }

    [Fact]
    public void Construtor_ArmazenaOHashRecebido()
    {
        var usuario = CriarUsuario();

        Assert.Equal(HashDaSenha, usuario.SenhaHash);
        Assert.NotEqual("segredo123", usuario.SenhaHash);
    }

    [Fact]
    public void Construtor_ComecaComContaNaoVerificada()
    {
        Assert.False(CriarUsuario().ContaVerificada);
    }

    [Fact]
    public void AlterarSenhaHash_RecusaHashVazio()
    {
        var usuario = CriarUsuario();

        Assert.Throws<ArgumentException>(() => usuario.AlterarSenhaHash(" "));
        Assert.Equal(HashDaSenha, usuario.SenhaHash);
    }

    [Fact]
    public void AlterarSenhaHash_SubstituiOEstado()
    {
        var usuario = CriarUsuario();
        string novoHash = new SenhaHasher().CriarHash("nova-senha");

        usuario.AlterarSenhaHash(novoHash);

        Assert.Equal(novoHash, usuario.SenhaHash);
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
