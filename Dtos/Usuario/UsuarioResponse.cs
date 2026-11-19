namespace Portfy.Dtos.Usuario;

/// Projeção de leitura do usuário. `SenhaHash` nunca entra em DTO de resposta
/// (arch §10, item 6).
public sealed class UsuarioResponse
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public decimal SalarioMensal { get; init; }
    public DateTime DataRecebimento { get; init; }
    public bool ContaVerificada { get; init; }
}
