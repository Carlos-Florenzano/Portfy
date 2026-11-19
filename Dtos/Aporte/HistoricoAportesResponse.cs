namespace Portfy.Dtos.Aporte;

/// Resposta do GET api/aporte: histórico do usuário no período pedido, com o
/// total já calculado no backend (o SPA não soma dinheiro — arch §6.2).
public sealed class HistoricoAportesResponse
{
    public decimal Total { get; init; }
    public int Quantidade { get; init; }
    public IReadOnlyCollection<AporteResponse> Aportes { get; init; } =
        Array.Empty<AporteResponse>();
}
