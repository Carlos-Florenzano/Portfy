namespace Portfy.Dtos.Orcamento;

/// Entrada do PUT api/orcamento/{id}: redefine o teto de uma categoria existente.
/// O nome da categoria é imutável para não quebrar o índice único (UsuarioId, NomeCategoria).
public sealed class AtualizarOrcamentoRequest
{
    public decimal LimiteDefinido { get; init; }
    public decimal PercentualDoSalario { get; init; }
    public bool EhFixo { get; init; }
}
