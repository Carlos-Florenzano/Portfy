namespace Portfy.Dtos.Ativo;

/// Projeção de leitura do catálogo. `Tipo` vai como string, igual ao contrato JSON
/// anterior (enum serializado como texto).
public sealed class AtivoResponse
{
    public string Ticker { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public decimal PrecoAtual { get; init; }
    public decimal RentabilidadeAcumulada { get; init; }
}
