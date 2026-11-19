namespace Portfy.Dtos.Relatorio;

/// Categoria de orçamento com o teto, o gasto e o saldo consolidados.
public sealed class OrcamentoRelatorioResponse
{
    public int Id { get; init; }
    public string NomeCategoria { get; init; } = string.Empty;
    public decimal LimiteDefinido { get; init; }
    public decimal ValorGastoAtual { get; init; }
    public decimal SaldoRestante { get; init; }
    public decimal PercentualDoSalario { get; init; }
    public bool Estourado { get; init; }
    public bool EhFixo { get; init; }
}
