namespace Portfy.Dtos.Usuario;

/// Corpo do POST api/usuario/login.
public sealed class LoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}
