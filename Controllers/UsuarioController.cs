using Microsoft.AspNetCore.Mvc;
using Portfy.Models;

namespace Portfy.Controllers;

// [ApiController]: Ativa recursos automáticos do ASP.NET Core para APIs REST:
// 1. Validação Automática: Retorna erro HTTP 400 (Bad Request) se os dados recebidos forem inválidos.
// 2. Mapeamento Automático: Identifica se os dados vêm do corpo da requisição (JSON) ou da URL.
// 3. Respostas Padronizadas: Formata mensagens de erro no padrão RFC 7807 (JSON estruturado para o frontend).
[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase // Se houver dúvidas sobre ControllerBase, checar em AporteController.
{
    // Mantém o estado do Usuário na memória do servidor (simulado para testes)
    private static readonly Usuario _usuarioPadrao = new Usuario("Carlos Florenzano", 5000m);

    // DTO (Data Transfer Object) para receber a alteração de salário vinda do frontend
    public class AtualizarSalarioRequest
    {
        public decimal NovoSalario { get; set; }
    }

    // GET: api/usuario
    // Retorna as informações cadastrais do usuário
    [HttpGet]
    public IActionResult ObterUsuario()
    {
        return Ok(new
        {
            // Ok(...): Método utilitário da ControllerBase que retorna o Status HTTP 200 OK.
            // new { ... }: Cria um Objeto Anônimo em C# no momento da execução.
            // O .NET converte automaticamente esse objeto anônimo em um texto JSON para o cliente (React/Swagger):
            // { "id": 1, "nome": "Carlos Florenzano", "salarioMensal": 5000.0 }
            Id = _usuarioPadrao.Id,
            Nome = _usuarioPadrao.Nome,
            SalarioMensal = _usuarioPadrao.SalarioMensal
        });
    }

    // PUT: api/usuario/salario
    // Atualiza o salário mensal do usuário
    [HttpPut("salario")]
    public IActionResult AtualizarSalario([FromBody] AtualizarSalarioRequest request)
    {
        if (request.NovoSalario <= 0)
        {
            return BadRequest(new { Mensagem = "O salário mensal deve ser um valor maior que zero." });
        }

        try
        {
            // Instancia a nova representação do usuário utilizando o construtor simplificado: Usuario(nome, salarioMensal)
            var usuarioAtualizado = new Usuario(_usuarioPadrao.Nome, request.NovoSalario);

            return Ok(new
            {
                Mensagem = "Salário atualizado com sucesso!",
                SalarioAnterior = _usuarioPadrao.SalarioMensal,
                NovoSalario = usuarioAtualizado.SalarioMensal
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }
}