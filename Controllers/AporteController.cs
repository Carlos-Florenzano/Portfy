using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Aporte;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// A criação do aporte (com a geração do Id) é do IAporteService, injetado.
// Aporte é dado financeiro do usuário autenticado: exige token (ADR-010).
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
        // Origem do salário inexistente: o serviço valida a integridade referencial
        // e lança ArgumentException, traduzida em 400 pelo ExceptionHandlingMiddleware.
        AporteSimulado novoAporte = _aporteService.Criar(request.Valor, request.OrigemSalarioId);

        return Ok(new CriarAporteResponse
        {
            Mensagem = "Aporte realizado com sucesso!",
            Aporte = new AporteResponse
            {
                Id = novoAporte.Id,
                DataAporte = novoAporte.DataAporte,
                ValorAportado = novoAporte.ValorAportado,
                OrigemSalarioId = novoAporte.OrigemSalarioId,
            },
        });
    }
}
