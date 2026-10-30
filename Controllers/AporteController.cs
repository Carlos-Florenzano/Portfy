using Microsoft.AspNetCore.Mvc;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// [ApiController] ativa a validação automática de payload; [Route] define api/<recurso>.
// A criação do aporte (com a geração do Id) é do IAporteService, injetado.
[ApiController]
[Route("api/[controller]")]
public class AporteController : ControllerBase
{
    private readonly IAporteService _aporteService;

    public AporteController(IAporteService aporteService)
    {
        _aporteService = aporteService ?? throw new ArgumentNullException(nameof(aporteService));
    }

    // DTO de entrada: precisa de set público porque quem preenche é o desserializador de JSON.
    public class AporteRequest
    {
        public decimal Valor { get; set; }
        public int OrigemSalarioId { get; set; }
    }

    // POST: api/aporte
    [HttpPost]
    public IActionResult AdicionarAporte([FromBody] AporteRequest request)
    {
        if (request.Valor <= 0)
            return BadRequest(new { Mensagem = "O valor do aporte deve ser maior que zero." });

        if (request.OrigemSalarioId <= 0)
            return BadRequest(new { Mensagem = "O ID da origem do salário deve ser um número positivo." });

        var novoAporte = _aporteService.Criar(request.Valor, request.OrigemSalarioId);

        return Ok(new
        {
            Mensagem = "Aporte realizado com sucesso!",
            Aporte = novoAporte
        });
    }
}
