namespace Portfy.Dtos.Ativo;

/// Resposta de GET api/ativo/{ticker}/rendimento. O `Tipo` é o discriminador que
/// deixa explícito qual implementação do contrato polimórfico respondeu.
public sealed class RendimentoEstimadoResponse
{
    public string Ticker { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public int Quantidade { get; init; }
    public decimal RendimentoMensalEstimado { get; init; }
}
