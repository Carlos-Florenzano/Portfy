namespace Portfy.Dtos.Orcamento;

/// Resposta do GET api/orcamento e base da resposta de despesa.
public sealed class OrcamentoResponse
{
    public string NomeCategoria { get; init; } = string.Empty;
    public decimal LimiteDefinido { get; init; }
    public decimal ValorGastoAtual { get; init; }
    public decimal PercentualDoSalario { get; init; }
    public decimal SaldoRestante { get; init; }
    public bool Estourado { get; init; }
}
