using Microsoft.AspNetCore.Mvc;
using Portfy.Dtos.Usuario;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// Borda HTTP do CRUD de usuários. Nenhum estado fica aqui: os dados vivem no
// SQLite via IUsuarioService, injetado pelo container (ver Program.cs).
// Validação de forma → FluentValidationFilter; exceção → ExceptionHandlingMiddleware.
[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
    }

    #region 1. CREATE (Cadastro)

    // POST: api/usuario/cadastro
    [HttpPost("cadastro")]
    public IActionResult Cadastrar([FromBody] CadastroRequest request)
    {
        if (_usuarioService.LoginJaExiste(request.Login))
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: "Esse login já está cadastrado.");

        Usuario novoUsuario = _usuarioService.Cadastrar(
            request.Nome,
            request.SalarioMensal,
            request.Login,
            request.Senha);

        // 201 Created com a URL do recurso criado no cabeçalho Location.
        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = novoUsuario.Id },
            MapearConfirmado(novoUsuario, "Usuário cadastrado com sucesso!"));
    }

    #endregion

    #region 2. READ (Consultas)

    // GET: api/usuario
    [HttpGet]
    public IActionResult ObterTodos()
    {
        IReadOnlyCollection<UsuarioResponse> usuarios = _usuarioService
            .ObterTodos()
            .Select(MapearUsuario)
            .ToList();

        return Ok(usuarios);
    }

    // GET: api/usuario/{id}
    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        Usuario? usuario = _usuarioService.ObterPorId(id);

        if (usuario is null)
            return UsuarioNaoEncontrado();

        return Ok(MapearUsuario(usuario));
    }

    #endregion

    #region 3. UPDATE (Atualizações)

    // PUT: api/usuario/{id} (Atualização de dados cadastrais)
    [HttpPut("{id}")]
    public IActionResult AtualizarUsuario(int id, [FromBody] AtualizarUsuarioRequest request)
    {
        Usuario? usuario = _usuarioService.AtualizarDados(id, request.Nome, request.SalarioMensal);

        if (usuario is null)
            return UsuarioNaoEncontrado();

        return Ok(new AtualizacaoUsuarioResponse
        {
            Mensagem = "Dados do usuário atualizados com sucesso!",
            Id = usuario.Id,
            Nome = usuario.Nome,
            SalarioMensal = usuario.SalarioMensal,
        });
    }

    // PUT: api/usuario/{id}/salario (Atualização específica do salário)
    [HttpPut("{id}/salario")]
    public IActionResult AtualizarSalario(int id, [FromBody] AtualizarSalarioRequest request)
    {
        Usuario? usuario = _usuarioService.ObterPorId(id);

        if (usuario is null)
            return UsuarioNaoEncontrado();

        decimal salarioAnterior = usuario.SalarioMensal;

        Usuario? atualizado = _usuarioService.AtualizarSalario(id, request.NovoSalario);

        if (atualizado is null)
            return UsuarioNaoEncontrado();

        return Ok(new AtualizacaoSalarioResponse
        {
            Mensagem = "Salário atualizado com sucesso!",
            SalarioAnterior = salarioAnterior,
            NovoSalario = atualizado.SalarioMensal,
        });
    }

    // PUT: api/usuario/{id}/senha (Alteração segura de senha)
    [HttpPut("{id}/senha")]
    public IActionResult AlterarSenha(int id, [FromBody] AlterarSenhaRequest request)
    {
        Usuario? usuario = _usuarioService.AlterarSenha(id, request.SenhaAtual, request.NovaSenha);

        if (usuario is null)
            return UsuarioNaoEncontrado();

        return Ok(new MensagemResponse { Mensagem = "Senha alterada com sucesso!" });
    }

    #endregion

    #region 4. DELETE (Remoção)

    // DELETE: api/usuario/{id}
    [HttpDelete("{id}")]
    public IActionResult ExcluirUsuario(int id)
    {
        Usuario? removido = _usuarioService.Excluir(id);

        if (removido is null)
            return UsuarioNaoEncontrado();

        return Ok(new MensagemResponse
        {
            Mensagem = $"Usuário '{removido.Nome}' removido com sucesso!",
        });
    }

    #endregion

    #region Ações de Autenticação / Conta

    // POST: api/usuario/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        Usuario? usuario = _usuarioService.Autenticar(request.Login, request.Senha);

        if (usuario is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Login ou senha incorretos.");

        if (!usuario.ContaVerificada)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "A conta ainda não foi verificada.");

        return Ok(MapearConfirmado(usuario, "Login realizado com sucesso!"));
    }

    // POST: api/usuario/{id}/verificar
    [HttpPost("{id}/verificar")]
    public IActionResult VerificarConta(int id)
    {
        Usuario? usuario = _usuarioService.VerificarConta(id);

        if (usuario is null)
            return UsuarioNaoEncontrado();

        return Ok(new VerificacaoContaResponse
        {
            Mensagem = "Conta verificada com sucesso!",
            Id = usuario.Id,
            ContaVerificada = usuario.ContaVerificada,
        });
    }

    #endregion

    private UsuarioResponse MapearUsuario(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Login = usuario.Login,
        SalarioMensal = usuario.SalarioMensal,
        DataRecebimento = usuario.DataRecebimento,
        ContaVerificada = usuario.ContaVerificada,
    };

    private UsuarioConfirmadoResponse MapearConfirmado(Usuario usuario, string mensagem) => new()
    {
        Mensagem = mensagem,
        Id = usuario.Id,
        Nome = usuario.Nome,
        Login = usuario.Login,
        ContaVerificada = usuario.ContaVerificada,
    };

    private ObjectResult UsuarioNaoEncontrado() =>
        Problem(statusCode: StatusCodes.Status404NotFound, detail: "Usuário não encontrado.");
}
