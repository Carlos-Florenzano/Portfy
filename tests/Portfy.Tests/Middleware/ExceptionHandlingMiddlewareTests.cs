using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Portfy.Middleware;
using Portfy.Services.Contracts;

namespace Portfy.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, string Corpo, string TipoDeConteudo)> ExecutarAsync(
        Exception excecao)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = "POST";
        contexto.Request.Path = "/api/usuario/cadastro";
        contexto.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw excecao,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(contexto);

        contexto.Response.Body.Position = 0;
        using var leitor = new StreamReader(contexto.Response.Body);
        string corpo = await leitor.ReadToEndAsync();

        return (contexto.Response.StatusCode, corpo, contexto.Response.ContentType ?? string.Empty);
    }

    [Fact]
    public async Task ArgumentException_Vira400ComMensagemDeDominio()
    {
        var resultado = await ExecutarAsync(new ArgumentException("O nome não pode ser vazio."));

        Assert.Equal(StatusCodes.Status400BadRequest, resultado.Status);
        Assert.Contains("application/problem+json", resultado.TipoDeConteudo);
        Assert.Contains("O nome não pode ser vazio.", resultado.Corpo);
    }

    [Fact]
    public async Task InvalidOperationException_Vira422()
    {
        var resultado = await ExecutarAsync(new InvalidOperationException("Saldo insuficiente."));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, resultado.Status);
        Assert.Contains("Saldo insuficiente.", resultado.Corpo);
    }

    [Fact]
    public async Task KeyNotFoundException_Vira404()
    {
        var resultado = await ExecutarAsync(new KeyNotFoundException());

        Assert.Equal(StatusCodes.Status404NotFound, resultado.Status);
    }

    [Fact]
    public async Task ExcecaoDesconhecida_Vira500SemVazarDetalheInterno()
    {
        var resultado = await ExecutarAsync(new Exception("detalhe interno sensível"));

        Assert.Equal(StatusCodes.Status500InternalServerError, resultado.Status);
        Assert.DoesNotContain("detalhe interno sensível", resultado.Corpo);
    }

    [Fact]
    public async Task ServicoExternoIndisponivel_Vira503()
    {
        var resultado = await ExecutarAsync(
            new ServicoExternoIndisponivelException("O provedor externo não respondeu a tempo."));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, resultado.Status);
        Assert.Contains("não respondeu a tempo", resultado.Corpo);
    }
}
