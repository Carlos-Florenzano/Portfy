using System.Text.Json;
using Microsoft.Extensions.Options;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;

namespace Portfy.Services.Integracoes;

/// Cotação de ações da B3, FIIs e cripto via BRAPI — a fonte rápida do mercado
/// nacional. A credencial viaja na query string; por isso nenhuma mensagem nem
/// log reproduz a URL da requisição.
public sealed class ProvedorBrapi : ProvedorHttpBase, IProvedorDeCotacoes
{
    private readonly OpcoesDeIntegracao _opcoes;
    private readonly IRelogio _relogio;

    public ProvedorBrapi(
        HttpClient cliente,
        IOptions<OpcoesDeIntegracao> opcoes,
        IRelogio relogio)
        : base(cliente)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        _opcoes = opcoes.Value;
        _relogio = relogio ?? throw new ArgumentNullException(nameof(relogio));
    }

    public OrigemDeMercado Origem => OrigemDeMercado.Brapi;

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_opcoes.TokenBrapi);

    public async Task<CotacaoResponse?> ObterCotacaoAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        string caminho = string.Concat(
            "/api/quote/", Uri.EscapeDataString(ticker),
            "?token=", Uri.EscapeDataString(_opcoes.TokenBrapi));

        using JsonDocument? documento = await ObterDocumentoAsync(caminho, cancellationToken);

        if (documento is null)
            return null;

        JsonElement raiz = documento.RootElement;

        // A BRAPI responde erro como objeto e sucesso com a lista em "results".
        if (raiz.ValueKind != JsonValueKind.Object ||
            !raiz.TryGetProperty("results", out JsonElement resultados) ||
            resultados.ValueKind != JsonValueKind.Array ||
            resultados.GetArrayLength() == 0)
            return null;

        JsonElement cotacao = resultados[0];
        decimal? preco = LeitorJson.Numero(cotacao, "regularMarketPrice");

        if (preco is null || preco <= 0m)
            return null;

        string? simbolo = LeitorJson.Texto(cotacao, "symbol");

        return new CotacaoResponse
        {
            Ticker = (simbolo ?? ticker).ToUpperInvariant(),
            Nome = LeitorJson.Texto(cotacao, "longName", "shortName") ?? simbolo ?? ticker,
            Preco = preco.Value,
            VariacaoPercentual = LeitorJson.Numero(
                cotacao, "regularMarketChangePercent", "regularMarketChange"),
            Moeda = LeitorJson.Texto(cotacao, "currency") ?? string.Empty,
            AtualizadaEm = LeitorJson.Data(cotacao, "regularMarketTime") ?? _relogio.UtcNow,
            Fonte = Origem.ToString(),
        };
    }
}
