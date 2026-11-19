namespace Portfy.Dtos.Usuario;

/// Resposta do cadastro e do login: é o mesmo contrato consumido pelo SPA
/// (`UsuarioConfirmado` em frontend/src/services/UsuarioService.ts).
public sealed class UsuarioConfirmadoResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public bool ContaVerificada { get; init; }
}
