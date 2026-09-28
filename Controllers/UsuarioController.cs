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
    // Mantém o estado dos Usuários na memória do servidor (simulado para testes)
    // ALTERADO: Antes existia apenas um usuário fixo.
    private static readonly List<Usuario> _usuarios = new List<Usuario>();

    // NOVO: Controla o próximo ID que será utilizado no cadastro.
    private static int _proximoId = 1;


    // DTO (Data Transfer Object) para receber os dados de cadastro vindos do frontend
    // NOVO
    public class CadastroRequest
    {
        public string Nome { get; set; } = string.Empty;
        public decimal SalarioMensal { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }


    // DTO (Data Transfer Object) para receber os dados de login vindos do frontend
    // NOVO
    public class LoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }


    // DTO (Data Transfer Object) para receber a alteração de salário vinda do frontend
    public class AtualizarSalarioRequest
    {
        public decimal NovoSalario { get; set; }
    }


    // POST: api/usuario/cadastro
    // Realiza o cadastro de um novo usuário
    // NOVO
    [HttpPost("cadastro")]
    public IActionResult Cadastrar([FromBody] CadastroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest(new
            {
                Mensagem = "O nome não pode ser vazio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Login))
        {
            return BadRequest(new
            {
                Mensagem = "O login não pode ser vazio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            return BadRequest(new
            {
                Mensagem = "A senha não pode ser vazia."
            });
        }

        if (request.SalarioMensal < 0)
        {
            return BadRequest(new
            {
                Mensagem = "O salário não pode ser negativo."
            });
        }

        // Verifica se já existe um usuário utilizando o mesmo login.
        // NOVO
        bool loginExiste = _usuarios.Any(usuario =>
            usuario.Login.Equals(
                request.Login,
                StringComparison.OrdinalIgnoreCase));

        if (loginExiste)
        {
            return Conflict(new
            {
                Mensagem = "Esse login já está cadastrado."
            });
        }

        try
        {
            // Instancia um novo usuário utilizando o construtor completo com login e senha.
            // NOVO
            Usuario novoUsuario = new Usuario(
                _proximoId,
                request.Nome,
                request.SalarioMensal,
                DateTime.Now,
                request.Login,
                request.Senha);

            _usuarios.Add(novoUsuario);
            _proximoId++;

            return Created("api/usuario/" + novoUsuario.Id, new
            {
                Mensagem = "Usuário cadastrado com sucesso!",
                Id = novoUsuario.Id,
                Nome = novoUsuario.Nome,
                Login = novoUsuario.Login,
                ContaVerificada = novoUsuario.ContaVerificada
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Mensagem = ex.Message
            });
        }
    }


    // POST: api/usuario/login
    // Realiza o login do usuário
    // NOVO
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Procura um usuário cadastrado utilizando o login informado.
        // NOVO
        Usuario? usuario = _usuarios.FirstOrDefault(usuario =>
            usuario.Login.Equals(
                request.Login,
                StringComparison.OrdinalIgnoreCase));

        if (usuario == null)
        {
            return Unauthorized(new
            {
                Mensagem = "Login ou senha incorretos."
            });
        }

        // Verifica se a senha informada corresponde à senha armazenada.
        // NOVO
        if (!usuario.VerificarSenha(request.Senha))
        {
            return Unauthorized(new
            {
                Mensagem = "Login ou senha incorretos."
            });
        }

        // Impede o login caso a conta ainda não tenha sido verificada.
        // NOVO
        if (!usuario.ContaVerificada)
        {
            return Unauthorized(new
            {
                Mensagem = "A conta ainda não foi verificada."
            });
        }

        return Ok(new
        {
            Mensagem = "Login realizado com sucesso!",
            Id = usuario.Id,
            Nome = usuario.Nome,
            Login = usuario.Login,
            ContaVerificada = usuario.ContaVerificada
        });
    }


    // POST: api/usuario/{id}/verificar
    // Realiza a verificação da conta do usuário
    // NOVO
    [HttpPost("{id}/verificar")]
    public IActionResult VerificarConta(int id)
    {
        // Procura o usuário pelo ID.
        // NOVO
        Usuario? usuario = _usuarios.FirstOrDefault(usuario =>
            usuario.Id == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                Mensagem = "Usuário não encontrado."
            });
        }

        // Altera o estado da conta para verificada.
        // NOVO
        usuario.VerificarConta();

        return Ok(new
        {
            Mensagem = "Conta verificada com sucesso!",
            Id = usuario.Id,
            ContaVerificada = usuario.ContaVerificada
        });
    }


    // GET: api/usuario
    // Retorna as informações cadastrais dos usuários
    // ALTERADO: Antes retornava apenas o usuário padrão.
    [HttpGet]
    public IActionResult ObterUsuario()
    {
        return Ok(_usuarios.Select(usuario => new
        {
            // Ok(...): Método utilitário da ControllerBase que retorna o Status HTTP 200 OK.
            // new { ... }: Cria um Objeto Anônimo em C# no momento da execução.
            // O .NET converte automaticamente esse objeto anônimo em um texto JSON para o cliente (React/Swagger).
            Id = usuario.Id,
            Nome = usuario.Nome,
            Login = usuario.Login,
            SalarioMensal = usuario.SalarioMensal,
            DataRecebimento = usuario.DataRecebimento,
            ContaVerificada = usuario.ContaVerificada
        }));
    }


    // PUT: api/usuario/{id}/salario
    // Atualiza o salário mensal do usuário
    // ALTERADO: Agora identifica qual usuário terá o salário alterado.
    [HttpPut("{id}/salario")]
    public IActionResult AtualizarSalario(
        int id,
        [FromBody] AtualizarSalarioRequest request)
    {
        // Procura o usuário pelo ID.
        // NOVO
        Usuario? usuario = _usuarios.FirstOrDefault(usuario =>
            usuario.Id == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                Mensagem = "Usuário não encontrado."
            });
        }

        if (request.NovoSalario <= 0)
        {
            return BadRequest(new
            {
                Mensagem = "O salário mensal deve ser um valor maior que zero."
            });
        }

        try
        {
            decimal salarioAnterior = usuario.SalarioMensal;

            // Atualiza o salário do próprio usuário.
            // ALTERADO
            usuario.AtualizarSalario(request.NovoSalario);

            return Ok(new
            {
                Mensagem = "Salário atualizado com sucesso!",
                SalarioAnterior = salarioAnterior,
                NovoSalario = usuario.SalarioMensal
            });
        }
        catch (ArgumentException ex)
        {
            // ALTERADO: Usuario.AtualizarSalario utiliza ArgumentException.
            return BadRequest(new
            {
                Mensagem = ex.Message
            });
        }
    }
}