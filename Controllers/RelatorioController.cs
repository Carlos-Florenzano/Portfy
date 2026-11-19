using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Relatorio;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// O relatório é consolidado pelo IRelatorioService, nunca recalculado aqui.
// Dado financeiro do usuário autenticado (arch §9.1): o id do dono vem do token.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RelatorioController : ControllerBase
{
    private readonly IRelatorioService _relatorioService;

    public RelatorioController(IRelatorioService relatorioService)
    {
        _relatorioService = relatorioService
            ?? throw new ArgumentNullException(nameof(relatorioService));
    }

    // GET: api/relatorio?inicio=2026-01-01&fim=2026-01-31
    [HttpGet]
    public IActionResult Gerar([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        ObjectResult? periodoInvalido = ValidarPeriodo(inicio, fim);

        if (periodoInvalido is not null)
            return periodoInvalido;

        RelatorioResponse relatorio = _relatorioService.Gerar(usuarioId.Value, inicio, fim);

        return Ok(relatorio);
    }

    // GET: api/relatorio/csv?inicio=...&fim=... — mesmo recorte, pronto para planilha.
    [HttpGet("csv")]
    public IActionResult ExportarCsv([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        ObjectResult? periodoInvalido = ValidarPeriodo(inicio, fim);

        if (periodoInvalido is not null)
            return periodoInvalido;

        string csv = _relatorioService.GerarCsv(usuarioId.Value, inicio, fim);

        // BOM UTF-8: o Excel pt-BR abre o arquivo com os acentos corretos.
        byte[] conteudo = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv);

        return File(conteudo, "text/csv; charset=utf-8", NomeDoArquivo(inicio, fim));
    }

    // O período é um recorte de fluxo (aportes); intervalo invertido é erro do cliente.
    private ObjectResult? ValidarPeriodo(DateOnly? inicio, DateOnly? fim)
    {
        if (inicio is null || fim is null || inicio.Value <= fim.Value)
            return null;

        return Problem(
            statusCode: StatusCodes.Status400BadRequest,
            detail: "A data inicial não pode ser posterior à data final.");
    }

    private static string NomeDoArquivo(DateOnly? inicio, DateOnly? fim)
    {
        string recorte = inicio is null && fim is null
            ? "completo"
            : $"{inicio?.ToString("yyyy-MM-dd") ?? "inicio"}_{fim?.ToString("yyyy-MM-dd") ?? "fim"}";

        return $"portfy-relatorio-{recorte}.csv";
    }
}
