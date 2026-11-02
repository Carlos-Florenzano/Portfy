namespace Portfy.Dtos.Usuario;

/// Corpo do POST api/usuario/cadastro. Setter público porque quem preenche é o
/// desserializador JSON; as regras ficam em CadastroRequestValidator.
public sealed class CadastroRequest
{
    public string Nome { get; set; } = string.Empty;
    public decimal SalarioMensal { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}
