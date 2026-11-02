using Portfy.Dtos.Aporte;

namespace Portfy.Dtos.Carteira;

/// Resposta do GET api/carteira.
public sealed class CarteiraResponse
{
    public decimal PatrimonioTotal { get; init; }
    public decimal SaldoDisponivel { get; init; }
    public IReadOnlyCollection<PosicaoResponse> Posicoes { get; init; } = Array.Empty<PosicaoResponse>();
    public IReadOnlyCollection<AporteResponse> Aportes { get; init; } = Array.Empty<AporteResponse>();
}
