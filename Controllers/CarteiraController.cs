using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Ativo;
using Portfy.Dtos.Aporte;
using Portfy.Dtos.Carteira;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// Borda HTTP: o controller só traduz requisição ↔ serviço injetado e decide o status.
// A carteira pertence ao usuário autenticado, então exige token (ADR-010).
[ApiController]
[Authorize]
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
        CarteiraSimulada carteira = _carteiraService.ObterCarteira();

        return Ok(new CarteiraResponse
        {
            PatrimonioTotal = carteira.CalcularPatrimonioTotal(),
            SaldoDisponivel = carteira.SaldoDisponivel,
            Posicoes = carteira.Posicoes.Select(MapearPosicao).ToList(),
            Aportes = carteira.Aportes.Select(MapearAporte).ToList(),
        });
    }

    private PosicaoResponse MapearPosicao(Posicao posicao) => new()
    {
        Id = posicao.Id,
        Ativo = new AtivoResponse
        {
            Ticker = posicao.Ativo.Ticker,
            Nome = posicao.Ativo.Nome,
            Tipo = posicao.Ativo.Tipo.ToString(),
            PrecoAtual = posicao.Ativo.PrecoAtual,
            RentabilidadeAcumulada = posicao.Ativo.RentabilidadeAcumulada,
        },
        Quantidade = posicao.Quantidade,
        PrecoMedio = posicao.PrecoMedio,
    };

    private AporteResponse MapearAporte(AporteSimulado aporte) => new()
    {
        Id = aporte.Id,
        DataAporte = aporte.DataAporte,
        ValorAportado = aporte.ValorAportado,
        OrigemSalarioId = aporte.OrigemSalarioId,
    };
}
