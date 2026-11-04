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

    // GET: api/orcamento — orçamento padrão (contrato preservado).
    [HttpGet]
    public IActionResult ObterOrcamento()
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        Orcamento orcamento = _orcamentoService.ObterOrcamento(usuarioId.Value);

        return Ok(MapearOrcamento(orcamento));
    }

    // GET: api/orcamento/todos — todas as categorias com os totais consolidados.
    [HttpGet("todos")]
    public IActionResult ObterTodos()
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        IReadOnlyCollection<Orcamento> orcamentos = _orcamentoService.ObterTodos(usuarioId.Value);

        return Ok(new ListaOrcamentosResponse
        {
            Orcamentos = orcamentos.Select(MapearOrcamento).ToList(),
            TotalLimiteDefinido = orcamentos.Sum(o => o.LimiteDefinido),
            TotalGastoAtual = orcamentos.Sum(o => o.ValorGastoAtual),
            TotalSaldoRestante = orcamentos.Sum(o => o.ObterSaldoRestante()),
            PercentualComprometido = orcamentos.Sum(o => o.PercentualDoSalario),
        });
    }

    // GET: api/orcamento/{id}
    [HttpGet("{id:int}")]
    public IActionResult ObterPorId(int id)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        Orcamento? orcamento = _orcamentoService.ObterPorId(usuarioId.Value, id);

        if (orcamento is null)
            return OrcamentoNaoEncontrado();

        return Ok(MapearOrcamento(orcamento));
    }

    // POST: api/orcamento — cria o teto de uma nova categoria.
    [HttpPost]
    public IActionResult Criar([FromBody] CriarOrcamentoRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        Orcamento orcamento = _orcamentoService.Criar(
            usuarioId.Value,
            request.NomeCategoria.Trim(),
            request.LimiteDefinido,
            request.PercentualDoSalario,
            request.EhFixo);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = orcamento.Id },
            MapearOrcamento(orcamento));
    }

    // PUT: api/orcamento/{id} — redefine nome, limite, percentual e recorrência.
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] AtualizarOrcamentoRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        Orcamento? orcamento = _orcamentoService.Atualizar(
            usuarioId.Value,
            id,
            request.NomeCategoria?.Trim(),
            request.LimiteDefinido,
            request.PercentualDoSalario,
            request.EhFixo);

        if (orcamento is null)
            return OrcamentoNaoEncontrado();

        return Ok(MapearOrcamento(orcamento));
    }

    // DELETE: api/orcamento/{id}
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        Orcamento? orcamento = _orcamentoService.ObterPorId(usuarioId.Value, id);

        if (orcamento is null)
            return OrcamentoNaoEncontrado();

        _orcamentoService.Excluir(usuarioId.Value, id);

        return Ok(new OrcamentoRemovidoResponse
        {
            Mensagem = $"Orçamento '{orcamento.NomeCategoria}' removido com sucesso!",
            Id = orcamento.Id,
            NomeCategoria = orcamento.NomeCategoria,
        });
    }

    // POST: api/orcamento/despesa — despesa no orçamento padrão (contrato preservado).
    [HttpPost("despesa")]
    public IActionResult AdicionarDespesa([FromBody] AdicionarDespesaRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        _orcamentoService.AdicionarDespesa(usuarioId.Value, request.Valor);

        Orcamento orcamento = _orcamentoService.ObterOrcamento(usuarioId.Value);

        return Ok(MapearDespesa(orcamento));
    }

    // POST: api/orcamento/{id}/despesa — despesa em uma categoria específica.
    [HttpPost("{id:int}/despesa")]
    public IActionResult AdicionarDespesaNaCategoria(int id, [FromBody] AdicionarDespesaRequest request)
    {
        int? usuarioId = User.ObterUsuarioId();

        if (usuarioId is null)
            return Forbid();

        Orcamento? orcamento = _orcamentoService.AdicionarDespesa(
            usuarioId.Value,
            id,
            request.Valor);

        if (orcamento is null)
            return OrcamentoNaoEncontrado();

        return Ok(MapearDespesa(orcamento));
    }

    private static OrcamentoResponse MapearOrcamento(Orcamento orcamento) => new()
    {
        Id = orcamento.Id,
        NomeCategoria = orcamento.NomeCategoria,
        LimiteDefinido = orcamento.LimiteDefinido,
        ValorGastoAtual = orcamento.ValorGastoAtual,
        PercentualDoSalario = orcamento.PercentualDoSalario,
        SaldoRestante = orcamento.ObterSaldoRestante(),
        Estourado = orcamento.ValidarEstouroOrcamento(),
        EhFixo = orcamento.EhFixo,
    };

    private static AdicionarDespesaResponse MapearDespesa(Orcamento orcamento) => new()
    {
        Mensagem = "Despesa adicionada com sucesso!",
        NomeCategoria = orcamento.NomeCategoria,
        ValorGastoAtual = orcamento.ValorGastoAtual,
        SaldoRestante = orcamento.ObterSaldoRestante(),
        Estourado = orcamento.ValidarEstouroOrcamento(),
    };

    private ObjectResult OrcamentoNaoEncontrado() =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            detail: "Orçamento não encontrado para o usuário autenticado.");
}
