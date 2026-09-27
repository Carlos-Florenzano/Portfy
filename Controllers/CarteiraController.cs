using Microsoft.AspNetCore.Mvc;
using Portfy.Models;

namespace Portfy.Controllers;


// Com o [ApiController], o .NET faz essa checagem automaticamente.
// Se o React enviar um campo com tipo errado (ex.: um texto onde deveria ser número)
// ou violar alguma regra de validação, a API interrompe o fluxo e responde na hora um erro 400 Bad Request
// antes mesmo de entrar no método.
[ApiController]

// O route vai definir um endereço e controller receberá o nome da classe.
// (se houver dúvidas sobre) essa parte, checkar o mesmo uso do Route em AporteController.cs
[Route("api/[controller]")]

// Classe ControllerBase, que faz parte do namespace Microsoft.AspNetCore.Mvc.
// Ela nos dá acesso a dezenas de utilitários prontos para responder chamadas web, como:
// Ok(...) $\rightarrow$ Retorna Status HTTP 200 OK com dados JSON.
// BadRequest(...) $\rightarrow$ Retorna Status HTTP 400 Bad Request com a mensagem de erro.
// NotFound(...) $\rightarrow$ Retorna Status HTTP 404 Not Found.
// Propriedades úteis como User (para saber quem está autenticado)
// e Response / Request (para manipular os cabeçalhos do pacote HTTP).
public class CarteiraController : ControllerBase
{

    // Inicia com saldo fictício de 10k para testes
    // private: Protege quem pode ver a variável (apenas o CarteiraController).
    // static: Protege a existência da variável (mantém os dados na memória entre chamadas HTTP).
    // readonly: Protege o ponteiro/referência da variável (impede que o objeto seja sobrescrito do zero).
    private static readonly CarteiraSimulada _carteira = new CarteiraSimulada(10000m);

    // Avisa à API que esse método só deve ser executado quando uma requisição chegar utilizando o método GET do protocolo HTTP.
    // (Checar exemplos dessa chamada em AporteController.cs)
    [HttpGet]


    
    // IActionResult: É a interface de retorno padrão do ASP.NET Core que envelopa a resposta da API
    // com um status HTTP (como 200 OK) e o conteúdo do corpo da resposta.
    public IActionResult ObterCarteira()
    {
        return Ok(new
        {
            PatrimonioTotal = _carteira.CalcularPatrimonioTotal(),
            SaldoDisponivel = _carteira.SaldoDisponivel,
            Posicoes = _carteira.Posicoes,
            Aportes = _carteira.Aportes
        });
    }
}