namespace Portfy.Dtos.Mercado;

/// Contrato padronizado de dados fundamentalistas. Os campos que a fonte não
/// publica ficam nulos: o consumidor distingue "não informado" de zero.
public sealed class FundamentoResponse
{
    public string Ticker { get; init; } = string.Empty;

    public string Nome { get; init; } = string.Empty;

    public string Setor { get; init; } = string.Empty;

    public string Industria { get; init; } = string.Empty;

    public string Pais { get; init; } = string.Empty;

    public string Moeda { get; init; } = string.Empty;

    public string Descricao { get; init; } = string.Empty;

    public decimal? CapitalizacaoDeMercado { get; init; }

    public decimal? PrecoLucro { get; init; }

    public decimal? LucroPorAcao { get; init; }

    public decimal? Beta { get; init; }

    public string Fonte { get; init; } = string.Empty;
}
