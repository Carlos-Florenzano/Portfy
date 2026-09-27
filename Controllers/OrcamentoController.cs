using Microsoft.AspNetCore.Mvc;
using Portfy.Models;

namespace Portfy.Controllers;

// [ApiController]: Ativa utilitários automáticos para REST (validação automática de dados e erros padronizados).
// [Route("api/[controller]")]: Define a URL base dinâmica substituindo [controller] pelo nome da classe (ex: api/orcamento).
[ApiController]
[Route("api/[controller]")]
public class OrcamentoController : ControllerBase
{
    // Mantém o estado da categoria de orçamento na memória (simulado)
    private static readonly Orcamento _orcamentoAlimentacao = new Orcamento("Alimentação", 1200m, 24m);

    // DTO (Data Transfer Object) para registrar uma nova despesa
    public class AdicionarDespesaRequest
    {
        public decimal Valor { get; set; }
    }

    // GET: api/orcamento
    // Retorna a visão geral da categoria de orçamento, limite e saldos
    [HttpGet]
    public IActionResult ObterOrcamento()
    {
        return Ok(new
        {
            NomeCategoria = _orcamentoAlimentacao.NomeCategoria,
            LimiteDefinido = _orcamentoAlimentacao.LimiteDefinido,
            ValorGastoAtual = _orcamentoAlimentacao.ValorGastoAtual,
            PercentualDoSalario = _orcamentoAlimentacao.PercentualDoSalario,
            SaldoRestante = _orcamentoAlimentacao.ObterSaldoRestante(),
            Estourado = _orcamentoAlimentacao.ValidarEstouroOrcamento()
        });
    }

    // POST: api/orcamento/despesa
    // Registra um gasto na categoria e atualiza o saldo
    [HttpPost("despesa")]
    public IActionResult AdicionarDespesa([FromBody] AdicionarDespesaRequest request)
    {
        if (request.Valor <= 0)
        {
            return BadRequest(new { Mensagem = "O valor da despesa deve ser maior que zero." });
        }

        try
        {
            // Executa o método de negócio do modelo Orcamento.cs: AdicionarDespesa(decimal valor)
            _orcamentoAlimentacao.AdicionarDespesa(request.Valor);

            return Ok(new
            {
                Mensagem = "Despesa adicionada com sucesso!",
                NomeCategoria = _orcamentoAlimentacao.NomeCategoria,
                ValorGastoAtual = _orcamentoAlimentacao.ValorGastoAtual,
                SaldoRestante = _orcamentoAlimentacao.ObterSaldoRestante(),
                Estourado = _orcamentoAlimentacao.ValidarEstouroOrcamento()
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }
}