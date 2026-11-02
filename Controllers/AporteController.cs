using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Aporte;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// A criação do aporte (com a geração do Id e a soma ao saldo) é do IAporteService,
// injetado. O dono da carteira é o usuário do token, nunca o id do corpo.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AporteController : ControllerBase
{
    private readonly IAporteService _aporteService;

    public AporteController(IAporteService aporteService)
    {
        _aporteService = aporteService ?? throw new ArgumentNullException(nameof(aporteService));
    }

    // POST: api/aporte
    [HttpPost]
    public IActionResult AdicionarAporte([FromBody] AporteRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        AporteRealizado realizado = _aporteService.Criar(
            usuarioId.Value,
            request.Valor,
            request.CarteiraId);

        AporteSimulado novoAporte = realizado.Aporte;

        return Ok(new CriarAporteResponse
        {
            Mensagem = "Aporte realizado com sucesso!",
            Aporte = new AporteResponse
            {
                Id = novoAporte.Id,
                DataAporte = novoAporte.DataAporte,
                ValorAportado = novoAporte.ValorAportado,
                OrigemSalarioId = novoAporte.OrigemSalarioId,
                CarteiraId = novoAporte.CarteiraId,
            },
            SaldoDisponivel = realizado.SaldoDisponivel,
            PatrimonioTotal = realizado.PatrimonioTotal,
        });
    }
}
