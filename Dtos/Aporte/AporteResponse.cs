namespace Portfy.Dtos.Aporte;

/// Projeção de leitura do aporte, usada pela carteira e pela resposta de criação.
public sealed class AporteResponse
{
    public int Id { get; init; }
    public DateTime DataAporte { get; init; }
    public decimal ValorAportado { get; init; }
    public int OrigemSalarioId { get; init; }
    public int CarteiraId { get; init; }
}
