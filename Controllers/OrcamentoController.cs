using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Orcamento;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// O orçamento é lido e alterado pelo IOrcamentoService, nunca por campo estático local.
// Dado financeiro do usuário autenticado (arch §9.1): o id do dono vem do token.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrcamentoController : ControllerBase
{
    private readonly IOrcamentoService _orcamentoService;

    public OrcamentoController(IOrcamentoService orcamentoService)
    {
        _orcamentoService = orcamentoService
            ?? throw new ArgumentNullException(nameof(orcamentoService));
    }

    // GET: api/orcamento
    [HttpGet]
    public IActionResult ObterOrcamento()
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        Orcamento orcamento = _orcamentoService.ObterOrcamento(usuarioId.Value);

        return Ok(new OrcamentoResponse
        {
            NomeCategoria = orcamento.NomeCategoria,
            LimiteDefinido = orcamento.LimiteDefinido,
            ValorGastoAtual = orcamento.ValorGastoAtual,
            PercentualDoSalario = orcamento.PercentualDoSalario,
            SaldoRestante = orcamento.ObterSaldoRestante(),
            Estourado = orcamento.ValidarEstouroOrcamento(),
            EhFixo = orcamento.EhFixo,
        });
    }

    // POST: api/orcamento/despesa
    [HttpPost("despesa")]
    public IActionResult AdicionarDespesa([FromBody] AdicionarDespesaRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        _orcamentoService.AdicionarDespesa(usuarioId.Value, request.Valor);

        Orcamento orcamento = _orcamentoService.ObterOrcamento(usuarioId.Value);

        return Ok(new AdicionarDespesaResponse
        {
            Mensagem = "Despesa adicionada com sucesso!",
            NomeCategoria = orcamento.NomeCategoria,
            ValorGastoAtual = orcamento.ValorGastoAtual,
            SaldoRestante = orcamento.ObterSaldoRestante(),
            Estourado = orcamento.ValidarEstouroOrcamento(),
        });
    }
}
