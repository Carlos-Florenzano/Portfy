using Microsoft.AspNetCore.Mvc;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// Borda HTTP do CRUD de usuários. Nenhum estado fica aqui: a coleção vive no
// IUsuarioService, injetado pelo container (ver Program.cs).
[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
    }

    #region DTOs (Data Transfer Objects)

    public class CadastroRequest
    {
        public string Nome { get; set; } = string.Empty;
        public decimal SalarioMensal { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class AtualizarUsuarioRequest
    {
        public string Nome { get; set; } = string.Empty;
        public decimal SalarioMensal { get; set; }
    }

    public class AtualizarSalarioRequest
    {
        public decimal NovoSalario { get; set; }
    }

    public class AlterarSenhaRequest
    {
        public string SenhaAtual { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }

    #endregion

    #region 1. CREATE (Cadastro)

    // POST: api/usuario/cadastro
    [HttpPost("cadastro")]
    public IActionResult Cadastrar([FromBody] CadastroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { Mensagem = "O nome não pode ser vazio." });

        if (string.IsNullOrWhiteSpace(request.Login))
            return BadRequest(new { Mensagem = "O login não pode ser vazio." });

        if (string.IsNullOrWhiteSpace(request.Senha))
            return BadRequest(new { Mensagem = "A senha não pode ser vazia." });

        if (request.SalarioMensal < 0)
            return BadRequest(new { Mensagem = "O salário não pode ser negativo." });

        if (_usuarioService.LoginJaExiste(request.Login))
            return Conflict(new { Mensagem = "Esse login já está cadastrado." });

        try
        {
            Usuario novoUsuario = _usuarioService.Cadastrar(
                request.Nome,
                request.SalarioMensal,
                request.Login,
                request.Senha);

            // 201 Created com a URL do recurso criado no cabeçalho Location
            return CreatedAtAction(nameof(ObterPorId), new { id = novoUsuario.Id }, new
            {
                Mensagem = "Usuário cadastrado com sucesso!",
                novoUsuario.Id,
                novoUsuario.Nome,
                novoUsuario.Login,
                novoUsuario.ContaVerificada
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }

    #endregion

    #region 2. READ (Consultas)

    // GET: api/usuario
    [HttpGet]
    public IActionResult ObterTodos()
    {
        var lista = _usuarioService.ObterTodos().Select(usuario => new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Login,
            usuario.SalarioMensal,
            usuario.DataRecebimento,
            usuario.ContaVerificada
        });

        return Ok(lista);
    }

    // GET: api/usuario/{id}
    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        Usuario? usuario = _usuarioService.ObterPorId(id);

        if (usuario is null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        return Ok(new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Login,
            usuario.SalarioMensal,
            usuario.DataRecebimento,
            usuario.ContaVerificada
        });
    }

    #endregion

    #region 3. UPDATE (Atualizações)

    // PUT: api/usuario/{id} (Atualização de dados cadastrais)
    [HttpPut("{id}")]
    public IActionResult AtualizarUsuario(int id, [FromBody] AtualizarUsuarioRequest request)
    {
        try
        {
            Usuario? usuario = _usuarioService.AtualizarDados(id, request.Nome, request.SalarioMensal);

            if (usuario is null)
                return NotFound(new { Mensagem = "Usuário não encontrado." });

            return Ok(new
            {
                Mensagem = "Dados do usuário atualizados com sucesso!",
                usuario.Id,
                usuario.Nome,
                usuario.SalarioMensal
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }

    // PUT: api/usuario/{id}/salario (Atualização específica do salário)
    [HttpPut("{id}/salario")]
    public IActionResult AtualizarSalario(int id, [FromBody] AtualizarSalarioRequest request)
    {
        Usuario? usuario = _usuarioService.ObterPorId(id);

        if (usuario is null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        if (request.NovoSalario <= 0)
            return BadRequest(new { Mensagem = "O salário mensal deve ser um valor maior que zero." });

        try
        {
            decimal salarioAnterior = usuario.SalarioMensal;

            _usuarioService.AtualizarSalario(id, request.NovoSalario);

            return Ok(new
            {
                Mensagem = "Salário atualizado com sucesso!",
                SalarioAnterior = salarioAnterior,
                NovoSalario = usuario.SalarioMensal
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }

    // PUT: api/usuario/{id}/senha (Alteração segura de senha)
    [HttpPut("{id}/senha")]
    public IActionResult AlterarSenha(int id, [FromBody] AlterarSenhaRequest request)
    {
        try
        {
            Usuario? usuario = _usuarioService.AlterarSenha(id, request.SenhaAtual, request.NovaSenha);

            if (usuario is null)
                return NotFound(new { Mensagem = "Usuário não encontrado." });

            return Ok(new { Mensagem = "Senha alterada com sucesso!" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensagem = ex.Message });
        }
    }

    #endregion

    #region 4. DELETE (Remoção)

    // DELETE: api/usuario/{id}
    [HttpDelete("{id}")]
    public IActionResult ExcluirUsuario(int id)
    {
        Usuario? removido = _usuarioService.Excluir(id);

        if (removido is null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        return Ok(new { Mensagem = $"Usuário '{removido.Nome}' removido com sucesso!" });
    }

    #endregion

    #region Ações de Autenticação / Conta

    // POST: api/usuario/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        Usuario? usuario = _usuarioService.Autenticar(request.Login, request.Senha);

        if (usuario is null)
            return Unauthorized(new { Mensagem = "Login ou senha incorretos." });

        if (!usuario.ContaVerificada)
            return Unauthorized(new { Mensagem = "A conta ainda não foi verificada." });

        return Ok(new
        {
            Mensagem = "Login realizado com sucesso!",
            usuario.Id,
            usuario.Nome,
            usuario.Login,
            usuario.ContaVerificada
        });
    }

    // POST: api/usuario/{id}/verificar
    [HttpPost("{id}/verificar")]
    public IActionResult VerificarConta(int id)
    {
        Usuario? usuario = _usuarioService.VerificarConta(id);

        if (usuario is null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        return Ok(new
        {
            Mensagem = "Conta verificada com sucesso!",
            usuario.Id,
            usuario.ContaVerificada
        });
    }

    #endregion
}
