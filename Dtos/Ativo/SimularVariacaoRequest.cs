namespace Portfy.Dtos.Ativo;

/// Corpo do POST api/ativo/{ticker}/simular. Percentual ausente = variação sorteada.
public sealed class SimularVariacaoRequest
{
    public decimal? PercentualCustomizado { get; set; }
}
