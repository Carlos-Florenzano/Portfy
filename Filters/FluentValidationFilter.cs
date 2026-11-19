using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Portfy.Filters;

/// Executa o `IValidator<T>` registrado para cada argumento da action antes dela.
/// Concentra a validação de forma num único ponto (arch §14.1) e devolve
/// ProblemDetails em pt-BR, preservando o 400 automático esperado pelo SPA.
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    private const string TipoDeConteudo = "application/problem+json";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        Dictionary<string, string[]> erros = await ValidarArgumentosAsync(context);

        if (erros.Count > 0)
        {
            var problema = new ValidationProblemDetails(erros)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida.",
                Detail = erros.Values.SelectMany(mensagens => mensagens).First(),
                Instance = context.HttpContext.Request.Path,
            };

            context.Result = new ObjectResult(problema)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { TipoDeConteudo },
            };
            return;
        }

        await next();
    }

    private static async Task<Dictionary<string, string[]>> ValidarArgumentosAsync(
        ActionExecutingContext context)
    {
        var erros = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (object? argumento in context.ActionArguments.Values)
        {
            if (argumento is null)
                continue;

            Type tipoDoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());

            if (context.HttpContext.RequestServices.GetService(tipoDoValidador) is not IValidator validador)
                continue;

            ValidationResult resultado =
                await validador.ValidateAsync(new ValidationContext<object>(argumento));

            foreach (ValidationFailure falha in resultado.Errors)
            {
                // As chaves acompanham o JSON camelCase para o cliente mapear o campo.
                string campo = JsonNamingPolicy.CamelCase.ConvertName(falha.PropertyName);

                if (!erros.TryGetValue(campo, out List<string>? mensagens))
                    erros[campo] = mensagens = new List<string>();

                mensagens.Add(falha.ErrorMessage);
            }
        }

        return erros.ToDictionary(
            par => par.Key,
            par => par.Value.ToArray(),
            StringComparer.Ordinal);
    }
}
