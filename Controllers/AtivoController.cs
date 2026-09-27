using Microsoft.AspNetCore.Mvc;
using Portfy.Models;

namespace Portfy.Controllers;


// [ApiController]: Ativa recursos automáticos do ASP.NET Core para APIs REST:
// 1. Validação Automática: Retorna erro HTTP 400 (Bad Request) se os dados recebidos forem inválidos.
// 2. Mapeamento Automático: Identifica se os dados vêm do corpo da requisição (JSON) ou da URL.
// 3. Respostas Padronizadas: Formata mensagens de erro no padrão RFC 7807 (JSON estruturado para o frontend).
[ApiController]
[Route("api/[controller]")]
public class AtivoController : ControllerBase
{
    // Simula uma lista/catálogo de ativos no servidor em memória
    private static readonly List<Ativo> _catalogoAtivos = new List<Ativo>
    {
        new Ativo("PETR4", "Petrobras PN", TipoAtivo.Acao, 38.50m),
        new Ativo("VALE3", "Vale ON", TipoAtivo.Acao, 62.10m),
        new Ativo("HGLG11", "CGHG Logística", TipoAtivo.Fii, 160.00m),
        new Ativo("BTC", "Bitcoin", TipoAtivo.Cripto, 350000.00m)
    };

    // DTO para receber percentual personalizado opcional na simulação
    public class SimularVariacaoRequest
    {
        public decimal? PercentualCustomizado { get; set; }
    }

    // GET: api/ativo
    // Retorna a lista completa do catálogo de ativos
    [HttpGet]
    public IActionResult ObterAtivos()
    {
        return Ok(_catalogoAtivos.Select(a => new
        {
            a.Ticker,
            a.Nome,
            Tipo = a.Tipo.ToString(),
            a.PrecoAtual,
            a.RentabilidadeSimulada
        }));
    }

    // GET: api/ativo/{ticker}
    // Procura e retorna um ativo específico pelo Ticker
    [HttpGet("{ticker}")]
    public IActionResult ObterPorTicker(string ticker)
    {
        var ativo = _catalogoAtivos.FirstOrDefault(a => a.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));

        if (ativo == null)
        {
            return NotFound(new { Mensagem = $"Ativo com o ticker '{ticker}' não foi encontrado no catálogo." });
        }

        return Ok(new
        {
            ativo.Ticker,
            ativo.Nome,
            Tipo = ativo.Tipo.ToString(),
            ativo.PrecoAtual,
            ativo.RentabilidadeSimulada
        });
    }

    // POST: api/ativo/{ticker}/simular
    // Executa a simulação de variação de preço (aleatória ou com percentual informado)
    [HttpPost("{ticker}/simular")]
    public IActionResult SimularVariacao(string ticker, [FromBody] SimularVariacaoRequest? request)
    {
        var ativo = _catalogoAtivos.FirstOrDefault(a => a.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));

        if (ativo == null)
        {
            return NotFound(new { Mensagem = $"Ativo com o ticker '{ticker}' não foi encontrado no catálogo." });
        }

        decimal precoAnterior = ativo.PrecoAtual;

        if (request?.PercentualCustomizado != null)
        {
            // Invoca a sobrecarga informando o percentual diretamente
            ativo.SimularVariacaoPreco(request.PercentualCustomizado.Value);
        }
        else
        {
            // Invoca o método de variação aleatória de -5% a +5%
            ativo.SimularVariacaoPreco();
        }

        return Ok(new
        {
            Mensagem = $"Simulação de variação executada com sucesso para {ativo.Ticker}!",
            PrecoAnterior = precoAnterior,
            NovoPreco = ativo.PrecoAtual,
            RentabilidadeSimulada = ativo.RentabilidadeSimulada
        });
    }
}