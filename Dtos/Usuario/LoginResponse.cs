namespace Portfy.Dtos.Usuario;

/// Resposta do login (ADR-010): token JWT + expiração + dados do usuário.
/// O SPA guarda o token e envia `Authorization: Bearer` em cada requisição.
public sealed class LoginResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiraEm { get; init; }
    public UsuarioResponse Usuario { get; init; } = new();
}
