using Microsoft.AspNetCore.Mvc;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// [ApiController] ativa utilidades REST; [Route] define api/<nome do recurso>.
// O orçamento é lido e alterado pelo IOrcamentoService, nunca por campo estático local.
[ApiController]
[Route("api/[controller]")]
public class OrcamentoController : ControllerBase
{
    private readonly IOrcamentoService _orcamentoService;

    public OrcamentoController(IOrcamentoService orcamentoService)
    {
        _orcamentoService = orcamentoService
            ?? throw new ArgumentNullException(nameof(orcamentoService));
    }

    // DTO de entrada para registrar uma nova despesa.
    public class AdicionarDespesaRequest
    {
        public decimal Valor { get; set; }
    }

    // GET: api/orcamento
    [HttpGet]
    public IActionResult ObterOrcamento()
    {
        var orcamento = _orcamentoService.ObterOrcamento();

        return Ok(new
        {
            orcamento.NomeCategoria,
            orcamento.LimiteDefinido,
            orcamento.ValorGastoAtual,
            orcamento.PercentualDoSalario,
            SaldoRestante = orcamento.ObterSaldoRestante(),
            Estourado = orcamento.ValidarEstouroOrcamento()
        });
    }

    // POST: api/orcamento/despesa
    [HttpPost("despesa")]
    public IActionResult AdicionarDespesa([FromBody] AdicionarDespesaRequest request)
    {
        if (request.Valor <= 0)
            return BadRequest(new { Mensagem = "O valor da despesa deve ser maior que zero." });

        try
        {
            _orcamentoService.AdicionarDespesa(request.Valor);

            var orcamento = _orcamentoService.ObterOrcamento();

            return Ok(new
            {
                Mensagem = "Despesa adicionada com sucesso!",
                orcamento.NomeCategoria,
                orcamento.ValorGastoAtual,
                SaldoRestante = orcamento.ObterSaldoRestante(),
                Estourado = orcamento.ValidarEstouroOrcamento()
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }
}
