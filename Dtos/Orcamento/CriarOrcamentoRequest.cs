namespace Portfy.Dtos.Orcamento;

/// Entrada do POST api/orcamento: cria um teto para uma categoria de gasto
/// pertencente ao usuário autenticado.
public sealed class CriarOrcamentoRequest
{
    public string NomeCategoria { get; init; } = string.Empty;
    public decimal LimiteDefinido { get; init; }
    public decimal PercentualDoSalario { get; init; }
    public bool EhFixo { get; init; }
}
