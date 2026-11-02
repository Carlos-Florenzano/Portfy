using System.Security.Cryptography;
using BCrypt.Net;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Hash de senha com BCrypt (ADR-018). Sem estado, por isso Singleton em Program.cs.
///
/// Compatibilidade: o banco pode conter hashes PBKDF2 (`salt:hash` em Base64) do
/// esquema anterior. Eles continuam verificando e são substituídos por BCrypt no
/// primeiro login bem-sucedido — trocar o algoritmo sem essa ponte bloquearia
/// todos os usuários já cadastrados.
public sealed class SenhaHasher : ISenhaHasher
{
    /// Custo do BCrypt: 2^12 rodadas internas, acima do mínimo recomendado (10).
    public const int FatorDeTrabalho = 12;

    private const string PrefixoBcrypt = "$2";
    private const int TamanhoHashBcrypt = 60;
    private const int IteracoesLegadas = 100_000;
    private const int TamanhoHashLegado = 32;

    public string CriarHash(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("A senha não pode ser vazia.", nameof(senha));

        return BCrypt.Net.BCrypt.HashPassword(senha, FatorDeTrabalho);
    }

    public bool Verificar(string senha, string hashArmazenado)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(hashArmazenado))
            return false;

        return EhHashBcrypt(hashArmazenado)
            ? VerificarBcrypt(senha, hashArmazenado)
            : VerificarHashLegado(senha, hashArmazenado);
    }

    public bool PrecisaAtualizar(string hashArmazenado) =>
        !EhHashBcrypt(hashArmazenado);

    // Formato do BCrypt: 60 caracteres com prefixo de versão. Hash truncado ou de
    // outra origem cai no verificador legado e é recusado sem lançar exceção.
    private bool EhHashBcrypt(string hashArmazenado) =>
        hashArmazenado.Length == TamanhoHashBcrypt &&
        hashArmazenado.StartsWith(PrefixoBcrypt, StringComparison.Ordinal);

    private bool VerificarBcrypt(string senha, string hashArmazenado)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hashArmazenado);
        }
        catch (SaltParseException)
        {
            // Hash corrompido é credencial inválida, não erro interno: o login
            // deve recusar (401) em vez de derrubar a requisição com 500.
            return false;
        }
    }

    private bool VerificarHashLegado(string senha, string hashArmazenado)
    {
        string[] partes = hashArmazenado.Split(':');

        if (partes.Length != 2)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(partes[0]);
            byte[] hashArmazenadoBytes = Convert.FromBase64String(partes[1]);

            if (hashArmazenadoBytes.Length != TamanhoHashLegado)
                return false;

            byte[] hashInformado = Rfc2898DeriveBytes.Pbkdf2(
                senha,
                salt,
                IteracoesLegadas,
                HashAlgorithmName.SHA256,
                TamanhoHashLegado);

            return CryptographicOperations.FixedTimeEquals(hashArmazenadoBytes, hashInformado);
        }
        catch (FormatException)
        {
            // Base64 inválido também é credencial inválida, não erro interno.
            return false;
        }
    }
}
