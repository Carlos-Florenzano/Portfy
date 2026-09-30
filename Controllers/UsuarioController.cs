using Microsoft.AspNetCore.Mvc;
using Portfy.Models;

namespace Portfy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    // Armazenamento em memória (simulado)
    private static readonly List<Usuario> _usuarios = new List<Usuario>();
    private static int _proximoId = 1;

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

        bool loginExiste = _usuarios.Any(u =>
            u.Login.Equals(request.Login, StringComparison.OrdinalIgnoreCase));

        if (loginExiste)
            return Conflict(new { Mensagem = "Esse login já está cadastrado." });

        try
        {
            var novoUsuario = new Usuario(
                _proximoId,
                request.Nome,
                request.SalarioMensal,
                DateTime.Now,
                request.Login,
                request.Senha);

            _usuarios.Add(novoUsuario);
            _proximoId++;

            // Retorna 201 Created com a URL do recurso criado no cabeçalho Location
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
        var lista = _usuarios.Select(usuario => new
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
        var usuario = _usuarios.FirstOrDefault(u => u.Id == id);

        if (usuario == null)
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
        var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
        if (usuario == null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        try
        {
            usuario.AtualizarNome(request.Nome);
            usuario.AtualizarSalario(request.SalarioMensal);

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
        var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
        if (usuario == null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        if (request.NovoSalario <= 0)
            return BadRequest(new { Mensagem = "O salário mensal deve ser um valor maior que zero." });

        try
        {
            decimal salarioAnterior = usuario.SalarioMensal;
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
            return BadRequest(new { Mensagem = ex.Message });
        }
    }

    // PUT: api/usuario/{id}/senha (Alteração segura de senha)
    [HttpPut("{id}/senha")]
    public IActionResult AlterarSenha(int id, [FromBody] AlterarSenhaRequest request)
    {
        var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
        if (usuario == null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        try
        {
            usuario.AlterarSenha(request.SenhaAtual, request.NovaSenha);
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
        var usuario = _usuarios.FirstOrDefault(u => u.Id == id);

        if (usuario == null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        _usuarios.Remove(usuario);

        return Ok(new { Mensagem = $"Usuário '{usuario.Nome}' removido com sucesso!" });
    }

    #endregion

    #region Ações de Autenticação / Conta

    // POST: api/usuario/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var usuario = _usuarios.FirstOrDefault(u =>
            u.Login.Equals(request.Login, StringComparison.OrdinalIgnoreCase));

        if (usuario == null || !usuario.VerificarSenha(request.Senha))
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
        var usuario = _usuarios.FirstOrDefault(u => u.Id == id);

        if (usuario == null)
            return NotFound(new { Mensagem = "Usuário não encontrado." });

        usuario.VerificarConta();

        return Ok(new
        {
            Mensagem = "Conta verificada com sucesso!",
            usuario.Id,
            usuario.ContaVerificada
        });
    }

    #endregion
}