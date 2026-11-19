using Portfy.Services;

namespace Portfy.Tests.Services;

public class SenhaHasherTests
{
    private readonly SenhaHasher _hasher = new();

    [Fact]
    public void CriarHash_GeraBcryptVerificavel()
    {
        string hash = _hasher.CriarHash("segredo123");

        Assert.StartsWith("$2", hash);
        Assert.NotEqual("segredo123", hash);
        Assert.True(_hasher.Verificar("segredo123", hash));
        Assert.False(_hasher.Verificar("outra-senha", hash));
    }

    [Fact]
    public void CriarHash_GeraSalDiferenteParaAMesmaSenha()
    {
        Assert.NotEqual(_hasher.CriarHash("segredo123"), _hasher.CriarHash("segredo123"));
    }

    [Fact]
    public void CriarHash_RecusaSenhaVazia()
    {
        Assert.Throws<ArgumentException>(() => _hasher.CriarHash(" "));
    }

    [Fact]
    public void CriarHash_UsaOFatorDeTrabalhoConfigurado()
    {
        var hasher = new SenhaHasher(11);

        // Formato BCrypt: $2a$<fator>$<salt+hash>.
        Assert.StartsWith("$2a$11$", hasher.CriarHash("segredo123"));
    }

    [Fact]
    public void Construtor_RecusaFatorDeTrabalhoForaDaFaixa()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SenhaHasher(9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SenhaHasher(15));
    }

    [Fact]
    public void Verificar_RecusaQualquerHashForaDoFormatoBcrypt()
    {
        // Sem ponte para esquemas anteriores: hash estranho é credencial inválida.
        Assert.False(_hasher.Verificar("segredo123", "nao-e-hash"));
        Assert.False(_hasher.Verificar("segredo123", "c2FsdA==:aGFzaA=="));
        Assert.False(_hasher.Verificar("segredo123", "$2a$12$hash-truncado"));
        Assert.False(_hasher.Verificar("segredo123", string.Empty));
    }
}
