namespace Portfy.Dtos.Aporte;

/// Corpo do POST api/aporte.
public sealed class AporteRequest
{
    public decimal Valor { get; set; }
    public int OrigemSalarioId { get; set; }
}
