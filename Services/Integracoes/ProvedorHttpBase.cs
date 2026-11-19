using System.Net;
using System.Text.Json;
using Portfy.Services.Contracts;

namespace Portfy.Services.Integracoes;

/// Base comum dos adaptadores HTTP: trata 404 como "não existe", converte falha
/// de transporte, status e conteúdo em `ServicoExternoIndisponivelException` e
/// nunca reproduz a URL — as fontes recebem a credencial na query string, então a
/// mensagem (que chega ao log e ao ProblemDetails) não pode conter o endereço.
public abstract class ProvedorHttpBase
{
    private readonly HttpClient _cliente;

    protected ProvedorHttpBase(HttpClient cliente)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
    }

    protected async Task<JsonDocument?> ObterDocumentoAsync(
        string caminho,
        CancellationToken cancellationToken)
    {
        string? conteudo = await ObterConteudoAsync(caminho, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(conteudo))
            return null;

        try
        {
            return JsonDocument.Parse(conteudo);
        }
        catch (JsonException)
        {
            throw new ServicoExternoIndisponivelException(
                "O provedor externo devolveu um conteúdo que não é JSON válido.");
        }
    }

    private async Task<string?> ObterConteudoAsync(
        string caminho,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage resposta = await _cliente
                .GetAsync(caminho, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!resposta.IsSuccessStatusCode)
                throw new ServicoExternoIndisponivelException(
                    $"O provedor externo respondeu {(int)resposta.StatusCode}.");

            return await resposta.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout do HttpClient: quem chamou não cancelou, a fonte não respondeu.
            throw new ServicoExternoIndisponivelException(
                "O provedor externo não respondeu a tempo.");
        }
        catch (HttpRequestException)
        {
            throw new ServicoExternoIndisponivelException(
                "Falha de rede ao consultar o provedor externo.");
        }
    }
}
