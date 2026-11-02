using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Ativo;
using Portfy.Models;
using Portfy.Services;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// O catálogo é público (leitura); a simulação altera preço persistido e exige token.
[ApiController]
[Route("api/[controller]")]
public class AtivoController : ControllerBase
{
    private readonly IAtivoService _ativoService;

    public AtivoController(IAtivoService ativoService)
    {
        _ativoService = ativoService ?? throw new ArgumentNullException(nameof(ativoService));
    }

    // GET: api/ativo
    [HttpGet]
    public IActionResult ObterAtivos() =>
        Ok(_ativoService.ObterTodos().Select(MapearAtivo).ToList());

    // GET: api/ativo/{ticker}
    [HttpGet("{ticker}")]
    public IActionResult ObterPorTicker(string ticker)
    {
        Ativo? ativo = _ativoService.ObterPorTicker(ticker);

        if (ativo is null)
            return AtivoNaoEncontrado(ticker);

        return Ok(MapearAtivo(ativo));
    }

    // POST: api/ativo/{ticker}/simular
    [HttpPost("{ticker}/simular")]
    [Authorize]
    public IActionResult SimularVariacao(string ticker, [FromBody] SimularVariacaoRequest? request)
    {
        VariacaoSimulada? variacao = _ativoService.SimularVariacao(ticker, request?.PercentualCustomizado);

        if (variacao is null)
            return AtivoNaoEncontrado(ticker);

        return Ok(new SimulacaoVariacaoResponse
        {
            Mensagem = $"Simulação de variação executada com sucesso para {variacao.Ativo.Ticker}!",
            PrecoAnterior = variacao.PrecoAnterior,
            NovoPreco = variacao.Ativo.PrecoAtual,
            RentabilidadeAcumulada = variacao.Ativo.RentabilidadeAcumulada,
        });
    }

    private AtivoResponse MapearAtivo(Ativo ativo) => new()
    {
        Ticker = ativo.Ticker,
        Nome = ativo.Nome,
        Tipo = ativo.Tipo.ToString(),
        PrecoAtual = ativo.PrecoAtual,
        RentabilidadeAcumulada = ativo.RentabilidadeAcumulada,
    };

    private ObjectResult AtivoNaoEncontrado(string ticker) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            detail: $"Ativo com o ticker '{ticker}' não foi encontrado no catálogo.");
}
