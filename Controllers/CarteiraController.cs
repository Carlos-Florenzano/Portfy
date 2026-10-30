using Microsoft.AspNetCore.Mvc;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// [ApiController] valida o corpo da requisição antes da action (400 automático).
// Borda HTTP: o controller só traduz requisição ↔ serviço injetado e decide o status.
[ApiController]
[Route("api/[controller]")]
public class CarteiraController : ControllerBase
{
    private readonly ICarteiraService _carteiraService;

    public CarteiraController(ICarteiraService carteiraService)
    {
        _carteiraService = carteiraService
            ?? throw new ArgumentNullException(nameof(carteiraService));
    }

    // GET: api/carteira
    [HttpGet]
    public IActionResult ObterCarteira()
    {
        var carteira = _carteiraService.ObterCarteira();

        return Ok(new
        {
            PatrimonioTotal = carteira.CalcularPatrimonioTotal(),
            SaldoDisponivel = carteira.SaldoDisponivel,
            Posicoes = carteira.Posicoes,
            Aportes = carteira.Aportes
        });
    }
}
