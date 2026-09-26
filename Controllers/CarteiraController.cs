using Microsoft.AspNetCore.Mvc;
using Portfy.Models;

namespace Portfy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarteiraController : ControllerBase
{
    private static readonly CarteiraSimulada _carteira = new CarteiraSimulada(10000m); // Inicia com saldo fictício de 10k para testes

    [HttpGet]
    public IActionResult ObterCarteira()
    {
        return Ok(new
        {
            PatrimonioTotal = _carteira.CalcularPatrimonioTotal(),
            SaldoDisponivel = _carteira.SaldoDisponivel,
            Posicoes = _carteira.Posicoes,
            Aportes = _carteira.Aportes
        });
    }
}