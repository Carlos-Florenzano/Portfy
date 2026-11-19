namespace Portfy.Dtos.Usuario;

/// Resposta do POST api/usuario/{id}/verificar.
public sealed class VerificacaoContaResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public int Id { get; init; }
    public bool ContaVerificada { get; init; }
}
