using BCrypt.Net;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Hash de senha com BCrypt (ADR-018). Somente BCrypt é aceito: o projeto não tem
/// base de usuários de esquema anterior a migrar, então qualquer hash fora do
/// formato BCrypt é tratado como credencial inválida. Sem estado → Singleton.
public sealed class SenhaHasher : ISenhaHasher
{
    /// Custo padrão do BCrypt: 2^12 rodadas internas, acima do mínimo recomendado (10).
    public const int FatorDeTrabalhoPadrao = 12;

    public const int FatorDeTrabalhoMinimo = 10;
    public const int FatorDeTrabalhoMaximo = 14;

    private const string PrefixoBcrypt = "$2";
    private const int TamanhoHashBcrypt = 60;

    private readonly int _fatorDeTrabalho;

    public SenhaHasher() : this(FatorDeTrabalhoPadrao)
    {
    }

    // O fator vem de Auth:Senha:FatorDeTrabalho e é validado na construção:
    // configuração inválida falha no startup em vez de gerar hash fraco.
    public SenhaHasher(int fatorDeTrabalho)
    {
        if (fatorDeTrabalho < FatorDeTrabalhoMinimo || fatorDeTrabalho > FatorDeTrabalhoMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fatorDeTrabalho),
                $"O fator de trabalho do BCrypt deve estar entre " +
                $"{FatorDeTrabalhoMinimo} e {FatorDeTrabalhoMaximo}.");
        }

        _fatorDeTrabalho = fatorDeTrabalho;
    }

    public string CriarHash(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("A senha não pode ser vazia.", nameof(senha));

        return BCrypt.Net.BCrypt.HashPassword(senha, _fatorDeTrabalho);
    }

    public bool Verificar(string senha, string hashArmazenado)
    {
        if (string.IsNullOrEmpty(senha) || !EhHashBcrypt(hashArmazenado))
            return false;

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

    // Formato do BCrypt: 60 caracteres com prefixo de versão. Hash truncado ou de
    // outra origem é recusado antes de chegar à biblioteca, que lançaria exceção.
    private bool EhHashBcrypt(string hashArmazenado) =>
        hashArmazenado.Length == TamanhoHashBcrypt &&
        hashArmazenado.StartsWith(PrefixoBcrypt, StringComparison.Ordinal);
}
