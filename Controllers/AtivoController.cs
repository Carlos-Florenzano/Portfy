using Microsoft.AspNetCore.Mvc;
using Portfy.Models;
using Portfy.Services;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// [ApiController] ativa validação automática e respostas de erro no padrão RFC 7807.
// O catálogo e a simulação de preço vivem no IAtivoService, injetado pelo container.
[ApiController]
[Route("api/[controller]")]
public class AtivoController : ControllerBase
{
    private readonly IAtivoService _ativoService;

    public AtivoController(IAtivoService ativoService)
    {
        _ativoService = ativoService ?? throw new ArgumentNullException(nameof(ativoService));
    }

    // DTO de entrada: aceita um percentual opcional na simulação.
    public class SimularVariacaoRequest
    {
        public decimal? PercentualCustomizado { get; set; }
    }

    // GET: api/ativo
    [HttpGet]
    public IActionResult ObterAtivos()
    {
        return Ok(_ativoService.ObterTodos().Select(a => new
        {
            a.Ticker,
            a.Nome,
            Tipo = a.Tipo.ToString(),
            a.PrecoAtual,
            a.RentabilidadeAcumulada
        }));
    }

    // GET: api/ativo/{ticker}
    [HttpGet("{ticker}")]
    public IActionResult ObterPorTicker(string ticker)
    {
        Ativo? ativo = _ativoService.ObterPorTicker(ticker);

        if (ativo is null)
            return NotFound(new { Mensagem = $"Ativo com o ticker '{ticker}' não foi encontrado no catálogo." });

        return Ok(new
        {
            ativo.Ticker,
            ativo.Nome,
            Tipo = ativo.Tipo.ToString(),
            ativo.PrecoAtual,
            ativo.RentabilidadeAcumulada
        });
    }

    // POST: api/ativo/{ticker}/simular
    [HttpPost("{ticker}/simular")]
    public IActionResult SimularVariacao(string ticker, [FromBody] SimularVariacaoRequest? request)
    {
        try
        {
            VariacaoSimulada? variacao = _ativoService.SimularVariacao(ticker, request?.PercentualCustomizado);

            if (variacao is null)
                return NotFound(new { Mensagem = $"Ativo com o ticker '{ticker}' não foi encontrado no catálogo." });

            return Ok(new
            {
                Mensagem = $"Simulação de variação executada com sucesso para {variacao.Ativo.Ticker}!",
                variacao.PrecoAnterior,
                NovoPreco = variacao.Ativo.PrecoAtual,
                variacao.Ativo.RentabilidadeAcumulada
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }
}
