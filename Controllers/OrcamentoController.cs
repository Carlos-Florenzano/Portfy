using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Orcamento;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// O orçamento é lido e alterado pelo IOrcamentoService, nunca por campo estático local.
// Dado financeiro do usuário autenticado: exigir token em todos os verbos (ADR-010).
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
        Orcamento orcamento = _orcamentoService.ObterOrcamento();

        return Ok(new OrcamentoResponse
        {
            NomeCategoria = orcamento.NomeCategoria,
            LimiteDefinido = orcamento.LimiteDefinido,
            ValorGastoAtual = orcamento.ValorGastoAtual,
            PercentualDoSalario = orcamento.PercentualDoSalario,
            SaldoRestante = orcamento.ObterSaldoRestante(),
            Estourado = orcamento.ValidarEstouroOrcamento(),
        });
    }

    // POST: api/orcamento/despesa
    [HttpPost("despesa")]
    public IActionResult AdicionarDespesa([FromBody] AdicionarDespesaRequest request)
    {
        _orcamentoService.AdicionarDespesa(request.Valor);

        Orcamento orcamento = _orcamentoService.ObterOrcamento();

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
