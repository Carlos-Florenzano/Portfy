namespace Portfy.Dtos.Mercado;

/// Resposta da coleção de indicadores. `Avisos` registra o que a fonte oficial
/// não entregou, para que uma série com problema não esconda as demais.
public sealed class IndicadoresResponse
{
    public IReadOnlyCollection<IndicadorResponse> Indicadores { get; init; } =
        Array.Empty<IndicadorResponse>();

    public IReadOnlyCollection<string> Avisos { get; init; } = Array.Empty<string>();
}
