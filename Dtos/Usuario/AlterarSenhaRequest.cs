namespace Portfy.Dtos.Usuario;

/// Corpo do PUT api/usuario/{id}/senha.
public sealed class AlterarSenhaRequest
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
}
