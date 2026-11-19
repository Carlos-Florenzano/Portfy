using Portfy.Dtos.Aporte;

namespace Portfy.Dtos.Carteira;

/// Resposta do GET api/carteira. `Id` e `Nome` identificam a simulação do
/// usuário autenticado; os valores são sempre lidos do SQLite, nunca calculados no SPA.
public sealed class CarteiraResponse
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public decimal PatrimonioTotal { get; init; }
    public decimal SaldoDisponivel { get; init; }
    public IReadOnlyCollection<PosicaoResponse> Posicoes { get; init; } = Array.Empty<PosicaoResponse>();
    public IReadOnlyCollection<AporteResponse> Aportes { get; init; } = Array.Empty<AporteResponse>();
}
