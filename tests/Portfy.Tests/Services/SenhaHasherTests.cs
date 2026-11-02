using System.Security.Cryptography;
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
    public void Verificar_AceitaHashLegadoPbkdf2()
    {
        string hashLegado = CriarHashLegado("segredo123");

        Assert.True(_hasher.Verificar("segredo123", hashLegado));
        Assert.False(_hasher.Verificar("outra-senha", hashLegado));
    }

    [Fact]
    public void Verificar_RecusaHashMalformadoSemLancarExcecao()
    {
        Assert.False(_hasher.Verificar("segredo123", "nao-e-hash"));
        Assert.False(_hasher.Verificar("segredo123", "base64-invalido:base64-invalido"));
        Assert.False(_hasher.Verificar("segredo123", "$2a$12$hash-truncado"));
    }

    [Fact]
    public void PrecisaAtualizar_SoParaHashLegado()
    {
        Assert.False(_hasher.PrecisaAtualizar(_hasher.CriarHash("segredo123")));
        Assert.True(_hasher.PrecisaAtualizar(CriarHashLegado("segredo123")));
        Assert.True(_hasher.PrecisaAtualizar(string.Empty));
    }

    private static string CriarHashLegado(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}
