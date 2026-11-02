namespace Portfy.Dtos.Aporte;

/// Resposta do POST api/aporte.
public sealed class CriarAporteResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public AporteResponse Aporte { get; init; } = new();
}
