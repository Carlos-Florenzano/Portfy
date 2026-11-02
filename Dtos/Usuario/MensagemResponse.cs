namespace Portfy.Dtos.Usuario;

/// Resposta de comandos sem corpo de dados (alteração de senha, exclusão).
public sealed class MensagemResponse
{
    public string Mensagem { get; init; } = string.Empty;
}
