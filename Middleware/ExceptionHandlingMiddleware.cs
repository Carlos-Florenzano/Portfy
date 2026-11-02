using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Portfy.Middleware;

/// Tradução única de exceção → ProblemDetails (arch §14.2, ADR-005). Nenhuma
/// action precisa de try/catch para decidir status HTTP: o mapa abaixo é a
/// única política de erro do backend.
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception excecao) when (!context.Response.HasStarted)
        {
            await EscreverProblemaAsync(context, excecao);
        }
    }

    private async Task EscreverProblemaAsync(HttpContext context, Exception excecao)
    {
        // Cancelamento do cliente não é falha do servidor: não há para quem responder.
        if (excecao is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Requisição cancelada pelo cliente em {Metodo} {Caminho}.",
                context.Request.Method,
                context.Request.Path);
            return;
        }

        (int status, string titulo, string detalhe) = Mapear(excecao);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                excecao,
                "Falha não tratada em {Metodo} {Caminho}.",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                excecao,
                "Requisição recusada em {Metodo} {Caminho} com status {Status}.",
                context.Request.Method,
                context.Request.Path,
                status);
        }

        // ProblemDetails escrito direto na resposta: o middleware não depende de
        // IProblemDetailsService estar registrado para conseguir responder um erro.
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = context.Request.Path,
        };

        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }

    private static (int Status, string Titulo, string Detalhe) Mapear(Exception excecao) =>
        excecao switch
        {
            ArgumentException argumento =>
                (StatusCodes.Status400BadRequest, "Requisição inválida.", argumento.Message),
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Recurso não encontrado.", "O recurso solicitado não existe."),
            InvalidOperationException operacao =>
                (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada.", operacao.Message),
            DbUpdateException atualizacao when ViolacaoDeUnicidade(atualizacao) =>
                (StatusCodes.Status409Conflict, "Conflito de unicidade.", "Já existe um registro com este valor."),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.",
                  "Não foi possível concluir a operação."),
        };

    // SQLITE_CONSTRAINT_UNIQUE (2067): o índice único de Login é a rede de
    // segurança da checagem de duplicidade feita na aplicação.
    private static bool ViolacaoDeUnicidade(DbUpdateException excecao) =>
        excecao.InnerException is SqliteException sqlite &&
        sqlite.SqliteExtendedErrorCode == 2067;
}
