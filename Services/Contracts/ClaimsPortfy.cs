namespace Portfy.Services.Contracts;

/// Nomes das claims do token emitido pelo Portfy. Constantes explícitas para que
/// emissão (AuthService) e leitura (controllers) usem exatamente a mesma chave.
public static class ClaimsPortfy
{
    public const string Identificador = "sub";
    public const string Login = "unique_name";
    public const string Nome = "name";
    public const string Jti = "jti";
}
