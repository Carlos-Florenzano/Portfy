namespace Portfy.Dtos.Usuario;

/// Corpo do PUT api/usuario/{id} (dados cadastrais).
public sealed class AtualizarUsuarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public decimal SalarioMensal { get; set; }
}
