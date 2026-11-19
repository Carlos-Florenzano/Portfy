using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Ativo;
using Portfy.Dtos.Aporte;
using Portfy.Dtos.Carteira;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// Borda HTTP: o controller só traduz requisição ↔ serviço injetado e decide o status.
// A carteira pertence ao usuário autenticado (arch §9.1), então o id do dono vem
// sempre do token, nunca da URL ou do corpo.
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
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        CarteiraSimulada carteira = _carteiraService.ObterCarteira(usuarioId.Value);

        return Ok(MapearCarteira(carteira));
    }

    // POST: api/carteira/compras
    [HttpPost("compras")]
    public IActionResult Comprar([FromBody] OperacaoCarteiraRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        OperacaoCarteira? operacao = _carteiraService.Comprar(
            usuarioId.Value,
            request.Ticker,
            request.Quantidade);

        if (operacao is null)
            return AtivoNaoEncontrado(request.Ticker);

        return Ok(MapearOperacao(operacao, "Compra realizada com sucesso!"));
    }

    // POST: api/carteira/vendas
    [HttpPost("vendas")]
    public IActionResult Vender([FromBody] OperacaoCarteiraRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        OperacaoCarteira? operacao = _carteiraService.Vender(
            usuarioId.Value,
            request.Ticker,
            request.Quantidade);

        if (operacao is null)
            return AtivoNaoEncontrado(request.Ticker);

        return Ok(MapearOperacao(operacao, "Venda realizada com sucesso!"));
    }

    private static CarteiraResponse MapearCarteira(CarteiraSimulada carteira) => new()
    {
        Id = carteira.Id,
        Nome = carteira.Nome,
        PatrimonioTotal = carteira.CalcularPatrimonioTotal(),
        SaldoDisponivel = carteira.SaldoDisponivel,
        Posicoes = carteira.Posicoes.Select(MapearPosicao).ToList(),
        Aportes = carteira.Aportes.Select(MapearAporte).ToList(),
    };

    private static OperacaoCarteiraResponse MapearOperacao(
        OperacaoCarteira operacao,
        string mensagem) => new()
        {
            Mensagem = mensagem,
            Ticker = operacao.Ticker,
            Quantidade = operacao.Quantidade,
            PrecoUnitario = operacao.PrecoUnitario,
            ValorTotal = operacao.ValorTotal,
            SaldoDisponivel = operacao.Carteira.SaldoDisponivel,
            PatrimonioTotal = operacao.Carteira.CalcularPatrimonioTotal(),
        };

    private static PosicaoResponse MapearPosicao(Posicao posicao) => new()
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

    private static AporteResponse MapearAporte(AporteSimulado aporte) => new()
    {
        Id = aporte.Id,
        DataAporte = aporte.DataAporte,
        ValorAportado = aporte.ValorAportado,
        OrigemSalarioId = aporte.OrigemSalarioId,
        CarteiraId = aporte.CarteiraId,
    };

    private ObjectResult AtivoNaoEncontrado(string ticker) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            detail: $"Ativo com o ticker '{ticker}' não foi encontrado no catálogo.");
}
