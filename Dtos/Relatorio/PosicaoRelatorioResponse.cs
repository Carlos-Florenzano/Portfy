namespace Portfy.Dtos.Relatorio;

/// Posição da carteira com os valores já consolidados pelo backend.
public sealed class PosicaoRelatorioResponse
{
    public string Ticker { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public int Quantidade { get; init; }
    public decimal PrecoMedio { get; init; }
    public decimal PrecoAtual { get; init; }
    public decimal ValorInvestido { get; init; }
    public decimal ValorAtual { get; init; }
    public decimal Resultado { get; init; }
    public decimal RentabilidadePercentual { get; init; }
}
