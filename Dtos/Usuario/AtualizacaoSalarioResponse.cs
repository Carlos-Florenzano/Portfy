namespace Portfy.Dtos.Usuario;

/// Resposta do PUT api/usuario/{id}/salario.
public sealed class AtualizacaoSalarioResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public decimal SalarioAnterior { get; init; }
    public decimal NovoSalario { get; init; }
}
