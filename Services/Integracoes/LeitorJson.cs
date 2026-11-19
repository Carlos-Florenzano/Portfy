using System.Globalization;
using System.Text.Json;

namespace Portfy.Services.Integracoes;

/// Leitura tolerante de payloads de terceiros. A FMP mantém duas gerações de
/// contrato (v3 e stable) com nomes diferentes para o mesmo campo; ler por lista
/// de aliases mantém o contrato do Portfy estável sem apostar em um único nome.
internal static class LeitorJson
{
    internal static string? Texto(JsonElement elemento, params string[] nomes)
    {
        foreach (string nome in nomes)
        {
            if (!elemento.TryGetProperty(nome, out JsonElement valor) ||
                valor.ValueKind != JsonValueKind.String)
                continue;

            string? texto = valor.GetString();

            if (!string.IsNullOrWhiteSpace(texto))
                return texto.Trim();
        }

        return null;
    }

    internal static decimal? Numero(JsonElement elemento, params string[] nomes)
    {
        foreach (string nome in nomes)
        {
            if (!elemento.TryGetProperty(nome, out JsonElement valor))
                continue;

            if (valor.ValueKind == JsonValueKind.Number && valor.TryGetDecimal(out decimal numero))
                return numero;

            if (valor.ValueKind == JsonValueKind.String &&
                decimal.TryParse(valor.GetString(), NumberStyles.Number,
                    CultureInfo.InvariantCulture, out decimal texto))
                return texto;
        }

        return null;
    }

    internal static DateTimeOffset? Data(JsonElement elemento, params string[] nomes)
    {
        foreach (string nome in nomes)
        {
            if (!elemento.TryGetProperty(nome, out JsonElement valor) ||
                valor.ValueKind != JsonValueKind.String)
                continue;

            if (DateTimeOffset.TryParse(valor.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out DateTimeOffset data))
                return data;
        }

        return null;
    }
}
