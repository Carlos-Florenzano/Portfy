namespace Portfy.Dtos.Usuario;

/// Resposta do PUT api/usuario/{id}.
public sealed class AtualizacaoUsuarioResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public decimal SalarioMensal { get; init; }
}
