namespace Portfy.Dtos.Orcamento;

/// Resposta do GET api/orcamento e base da resposta de despesa.
public sealed class OrcamentoResponse
{
    // Identidade estável da categoria: o SPA usa o Id para editar e excluir.
    // Campo aditivo, não altera o contrato anterior de GET api/orcamento.
    public int Id { get; init; }

    public string NomeCategoria { get; init; } = string.Empty;
    public decimal LimiteDefinido { get; init; }
    public decimal ValorGastoAtual { get; init; }
    public decimal PercentualDoSalario { get; init; }
    public decimal SaldoRestante { get; init; }
    public bool Estourado { get; init; }
    public bool EhFixo { get; init; }
}
