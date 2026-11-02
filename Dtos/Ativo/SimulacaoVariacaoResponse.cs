namespace Portfy.Dtos.Ativo;

/// Resposta do POST api/ativo/{ticker}/simular, com o preço antes e depois.
public sealed class SimulacaoVariacaoResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public decimal PrecoAnterior { get; init; }
    public decimal NovoPreco { get; init; }
    public decimal RentabilidadeAcumulada { get; init; }
}
