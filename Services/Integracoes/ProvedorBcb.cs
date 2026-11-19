using System.Globalization;
using System.Text.Json;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;

namespace Portfy.Services.Integracoes;

/// Indicadores macroeconômicos oficiais via SGS (Banco Central). É fonte primária
/// e única desse dado: não existe fonte alternativa para substituí-la.
public sealed class ProvedorBcb : ProvedorHttpBase, IProvedorDeIndicadores
{
    // Série oficial do SGS para cada indicador suportado. Código errado aqui vira
    // dado errado no produto, por isso a tabela fica explícita e testada.
    private static readonly IReadOnlyDictionary<IndicadorEconomico, SerieSgs> Series =
        new Dictionary<IndicadorEconomico, SerieSgs>
        {
            [IndicadorEconomico.Ipca] = new("433", "IPCA — variação mensal", "% no mês"),
            [IndicadorEconomico.Selic] = new("432", "Selic — acumulada no mês", "% no mês"),
            [IndicadorEconomico.Cdi] = new("4389", "CDI — acumulado no mês", "% no mês"),
            [IndicadorEconomico.Dolar] = new("1", "Dólar comercial — taxa de venda", "BRL"),
        };

    public ProvedorBcb(HttpClient cliente)
        : base(cliente)
    {
    }

    public OrigemDeMercado Origem => OrigemDeMercado.BcbSgs;

    public async Task<IndicadorResponse?> ObterIndicadorAsync(
        IndicadorEconomico indicador,
        CancellationToken cancellationToken)
    {
        if (!Series.TryGetValue(indicador, out SerieSgs? serie))
            throw new ArgumentOutOfRangeException(
                nameof(indicador), indicador, "Indicador sem série do SGS mapeada.");

        string caminho = $"/dados/serie/bcdata.sgs.{serie.Codigo}/dados/ultimos/1?formato=json";

        using JsonDocument? documento = await ObterDocumentoAsync(caminho, cancellationToken);

        if (documento is null)
            return null;

        JsonElement raiz = documento.RootElement;

        if (raiz.ValueKind != JsonValueKind.Array)
            throw new ServicoExternoIndisponivelException(
                "O Banco Central devolveu um formato inesperado.");

        if (raiz.GetArrayLength() == 0)
            return null;

        JsonElement ultimo = raiz[raiz.GetArrayLength() - 1];
        decimal? valor = LerValor(LeitorJson.Texto(ultimo, "valor"));
        string? textoDaData = LeitorJson.Texto(ultimo, "data");

        if (valor is null ||
            !DateOnly.TryParseExact(textoDaData, "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly data))
            return null;

        return new IndicadorResponse
        {
            Codigo = indicador.ToString().ToUpperInvariant(),
            Descricao = serie.Descricao,
            Valor = valor.Value,
            Data = data,
            Unidade = serie.Unidade,
            SerieSgs = serie.Codigo,
            Fonte = Origem.ToString(),
        };
    }

    /// O SGS publica o valor como texto com ponto decimal (ex.: "0.82").
    private static decimal? LerValor(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture,
            out decimal valor) ? valor : null;
    }

    /// Código numérico do SGS, descrição oficial curta e unidade do valor.
    private sealed record SerieSgs(string Codigo, string Descricao, string Unidade);
}
