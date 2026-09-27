using System;
using Microsoft.AspNetCore.Mvc;
using Portfy.Models;

namespace Portfy.Controllers;

// Esse atributo ativa comportamentos automáticos no .NET específicos para APIs Web RESTful.
[ApiController]

// Esse atributo define o modelo da URL em que os métodos desse controller vão responder.
// O trecho "api/": Define um prefixo fixo para a rota.
// O token "[controller]": É uma variável dinâmica
// que o .NET substitui automaticamente pelo nome da classe (removendo o sufixo Controller).
[Route("api/[controller]")]


// Classe ControllerBase, que faz parte do namespace Microsoft.AspNetCore.Mvc.
// Ela nos dá acesso a dezenas de utilitários prontos para responder chamadas web, como:
// Ok(...) $\rightarrow$ Retorna Status HTTP 200 OK com dados JSON.
// BadRequest(...) $\rightarrow$ Retorna Status HTTP 400 Bad Request com a mensagem de erro.
// NotFound(...) $\rightarrow$ Retorna Status HTTP 404 Not Found.
// Propriedades úteis como User (para saber quem está autenticado)
// e Response / Request (para manipular os cabeçalhos do pacote HTTP).
public class AporteController : ControllerBase
{
    // Contador estático simulando o auto-incremento do ID (sem banco de dados)
    private static int _proximoId = 1;

    // Modelo de entrada para o request HTTP
    public class AporteRequest
    {
        // No caso das classes DTO (Data Transfer Objects) como a AporteRequest,
        // a propriedade precisa ter set público (ou init)
        // porque o responsável por preencher esses valores não é o código C#,
        // mas sim o desserializador de JSON do .NET (System.Text.Json).
        public decimal Valor { get; set; }
        public int OrigemSalarioId { get; set; }
    }


    // O [HttpPost] é um atributo de ação que define dois comportamentos fundamentais para o método que está logo abaixo dele:
    // Avisa ao .NET que esse método específico C# só deve ser executado quando chegar
    // uma requisição web utilizando o método POST do protocolo HTTP.
    // Se o cliente (como o React ou a barra de endereço do seu navegador) fizer uma chamada utilizando GET, PUT ou DELETE
    // para essa mesma URL (/api/Aporte), o .NET não executará esse método e retornará um erro 405 Method Not Allowed.
    // No padrão REST, cada verbo HTTP tem uma intenção clara:
    // [HttpGet]: Para consulta/leitura de dados (não altera o estado da aplicação).
    // [HttpPost]: Para criação, cadastro ou envio de dados novos
    // que devem alterar o estado da aplicação (como adicionar um novo aporte ou enviar um formulário).
    [HttpPost]


    // public: Indica que o método pode ser chamado publicamente pelo framework do .NET quando uma requisição chegar na rota.
    // IActionResult: É uma interface padrão do ASP.NET Core para respostas de APIs HTTP.
    // Ela permite retornar dados em formato JSON acompanhados de um código de status HTTP adequado.
    // Se der tudo certo, você retorna Ok(...) (Status HTTP 200) ou CreatedAtAction(...) (Status HTTP 201).
    // Se o cliente enviar dados inválidos, você retorna BadRequest(...) (Status HTTP 400).
    // Se o recurso não existir, retorna NotFound(...) (Status HTTP 404).
    // AdicionarAporte é o nome do método.
    // [FromBody] AporteRequest request:
    // Esta parte indica como a informação enviada pelo cliente (React, Swagger, etc.) deve ser capturada e convertida:
    // AporteRequest request: Declara uma variável chamada request do tipo da classe DTO (AporteRequest)
    // para guardar os dados da requisição (Valor e OrigemSalarioId).
    // [FromBody]: É um atributo que instrui o .NET a ler o corpo (body) da requisição HTTP — que vem formatado
    // como um texto JSON — e fazer a conversão automática (desserialização) para o objeto AporteRequest em C#.
    public IActionResult AdicionarAporte([FromBody] AporteRequest request)
    {
        if (request.Valor <= 0) // se o dado dentro de Valor, capturado do cliente e convertido, for menor ou igual a zero..
        {
            return BadRequest(new { Mensagem = "O valor do aporte deve ser maior que zero." });
        }

        if (request.OrigemSalarioId <= 0) // se o dado dentro de OrigemSalarioID, capturado e convertido, for menor ou igual a 0..
        {
            return BadRequest(new { Mensagem = "O ID da origem do salário deve ser um número positivo." });
        }

        // Instancia o modelo usando a assinatura correta do construtor
        // E sim, essa é uma invocação do construtor da classe AporteSimulado em AporteSimulado.cs
        var novoAporte = new AporteSimulado(
            id: _proximoId++, // nessa próxima chamada, acrescenta-se +1 ao valor armazenado em id
            valorAportado: request.Valor, // valorAportado recebe o conteúdo de Valor requisitado em AdicionarAporte
            origemSalarioId: request.OrigemSalarioId // origemSalarioId recebe o conteúdo de OrigemSalarioId em AdicionarAporte
        );

        // return para caso os ifs em AdicionarAporte tenham sido cumpridos com sucesso:
        return Ok(new
        {
            Mensagem = "Aporte realizado com sucesso!",
            Aporte = novoAporte // aporte vai armazenar o id, valor aportado e origemsalario
            // como dados de Aporte para caso o sistema precise consultar
        });
    }
}