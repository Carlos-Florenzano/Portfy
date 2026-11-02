using System.Security.Claims;

namespace Portfy.Services.Contracts;

/// Nomes das claims do token emitido pelo Portfy. Constantes explícitas para que
/// emissão (AuthService) e leitura (controllers) usem exatamente a mesma chave.
public static class ClaimsPortfy
{
    public const string Identificador = "sub";
    public const string Login = "unique_name";
    public const string Nome = "name";
    public const string Jti = "jti";

    /// Lê o id do usuário da claim "sub". Retorna null quando o token não a traz,
    /// e nesse caso o controller responde 403 em vez de escolher um usuário qualquer.
    public static int? ObterUsuarioId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(Identificador), out int id) ? id : null;
}
