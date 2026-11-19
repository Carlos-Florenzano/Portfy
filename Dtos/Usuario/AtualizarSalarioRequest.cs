namespace Portfy.Dtos.Usuario;

/// Corpo do PUT api/usuario/{id}/salario.
public sealed class AtualizarSalarioRequest
{
    public decimal NovoSalario { get; set; }
}
