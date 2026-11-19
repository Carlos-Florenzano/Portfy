namespace Portfy.Dtos.Mercado;

/// Contrato padronizado de indicador macroeconômico do Banco Central (SGS):
/// o valor mais recente publicado para a série, com a data a que ele se refere.
public sealed class IndicadorResponse
{
    public string Codigo { get; init; } = string.Empty;

    public string Descricao { get; init; } = string.Empty;

    public decimal Valor { get; init; }

    public DateOnly Data { get; init; }

    public string Unidade { get; init; } = string.Empty;

    public string SerieSgs { get; init; } = string.Empty;

    public string Fonte { get; init; } = string.Empty;
}
