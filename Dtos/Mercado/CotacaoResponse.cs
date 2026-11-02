namespace Portfy.Dtos.Mercado;

/// Contrato padronizado de cotação: BRAPI e FMP devolvem formatos diferentes,
/// mas o consumidor recebe sempre esta mesma forma e sabe a origem em `Fonte`.
public sealed class CotacaoResponse
{
    public string Ticker { get; init; } = string.Empty;

    public string Nome { get; init; } = string.Empty;

    public decimal Preco { get; init; }

    /// Nulo quando a fonte não informa a variação: ausência nunca vira zero.
    public decimal? VariacaoPercentual { get; init; }

    /// Vazio quando a fonte não informa a moeda; o domínio não adivinha.
    public string Moeda { get; init; } = string.Empty;

    /// Carimbo da fonte quando ela informa; caso contrário, o instante da consulta.
    public DateTimeOffset? AtualizadaEm { get; init; }

    public string Fonte { get; init; } = string.Empty;
}
