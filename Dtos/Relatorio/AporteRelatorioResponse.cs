namespace Portfy.Dtos.Relatorio;

/// Linha de aporte dentro do relatório consolidado.
public sealed class AporteRelatorioResponse
{
    public int Id { get; init; }
    public DateTime DataAporte { get; init; }
    public decimal ValorAportado { get; init; }
}
