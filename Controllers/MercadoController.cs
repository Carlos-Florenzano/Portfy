using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// Dados de mercado são leitura pública, como o catálogo de ativos. O consumidor
// deste controller não conhece FMP, BRAPI nem SGS: recebe o contrato padronizado.
[ApiController]
[Route("api/mercado")]
[EnableRateLimiting(PoliticasDeRateLimit.Mercado)]
public class MercadoController : ControllerBase
{
    private readonly IMercadoService _mercadoService;

    public MercadoController(IMercadoService mercadoService)
    {
        _mercadoService = mercadoService ?? throw new ArgumentNullException(nameof(mercadoService));
    }

    // GET: api/mercado/cotacoes/PETR4
    [HttpGet("cotacoes/{ticker}")]
    public async Task<IActionResult> ObterCotacao(string ticker, CancellationToken cancellationToken) =>
        Ok(await _mercadoService.ObterCotacaoAsync(ticker, cancellationToken));

    // GET: api/mercado/fundamentos/AAPL
    [HttpGet("fundamentos/{ticker}")]
    public async Task<IActionResult> ObterFundamentos(
        string ticker,
        CancellationToken cancellationToken) =>
        Ok(await _mercadoService.ObterFundamentosAsync(ticker, cancellationToken));

    // GET: api/mercado/indicadores
    [HttpGet("indicadores")]
    public async Task<IActionResult> ObterIndicadores(CancellationToken cancellationToken) =>
        Ok(await _mercadoService.ObterIndicadoresAsync(cancellationToken));
}
