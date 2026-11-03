namespace Portfy.Dtos.Orcamento;

/// Resposta do GET api/orcamento/todos. Os totais são calculados no backend:
/// o SPA apenas formata, nunca recalcula valor financeiro (arch §6.2).
public sealed class ListaOrcamentosResponse
{
    public IReadOnlyCollection<OrcamentoResponse> Orcamentos { get; init; } =
        Array.Empty<OrcamentoResponse>();

    public decimal TotalLimiteDefinido { get; init; }
    public decimal TotalGastoAtual { get; init; }
    public decimal TotalSaldoRestante { get; init; }

    /// Soma dos percentuais do salário comprometidos pelas categorias cadastradas.
    public decimal PercentualComprometido { get; init; }
}
