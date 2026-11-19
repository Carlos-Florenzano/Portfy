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

    // GET: api/aporte?inicio=AAAA-MM-DD&fim=AAAA-MM-DD
    [HttpGet]
    public IActionResult ObterHistorico(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        HistoricoDeAportes historico = _aporteService.ObterHistorico(
            usuarioId.Value,
            inicio,
            fim);

        return Ok(new HistoricoAportesResponse
        {
            Total = historico.Total,
            Quantidade = historico.Aportes.Count,
            Aportes = historico.Aportes.Select(MapearAporte).ToList(),
        });
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
            Aporte = MapearAporte(novoAporte),
            SaldoDisponivel = realizado.SaldoDisponivel,
            PatrimonioTotal = realizado.PatrimonioTotal,
        });
    }

    private static AporteResponse MapearAporte(AporteSimulado aporte) => new()
    {
        Id = aporte.Id,
        DataAporte = aporte.DataAporte,
        ValorAportado = aporte.ValorAportado,
        OrigemSalarioId = aporte.OrigemSalarioId,
        CarteiraId = aporte.CarteiraId,
    };
}
