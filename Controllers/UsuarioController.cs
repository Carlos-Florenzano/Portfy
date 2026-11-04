using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfy.Dtos.Usuario;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Controllers;

// Borda HTTP do CRUD de usuários. Nenhum estado fica aqui: os dados vivem no
// SQLite via IUsuarioService, injetado pelo container (ver Program.cs).
// Validação de forma → FluentValidationFilter; exceção → ExceptionHandlingMiddleware.
// Autorização: todo endpoint exige token, exceto cadastro, login e verificação de
// conta (esta última é chamada pelo fluxo de cadastro ainda sem sessão).
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IAuthService _authService;

    public UsuarioController(IUsuarioService usuarioService, IAuthService authService)
    {
        _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    #region 1. CREATE (Cadastro)

    // POST: api/usuario/cadastro
    [HttpPost("cadastro")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeRateLimit.Cadastro)]
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

    // GET: api/usuario — devolve apenas o usuário do token. A listagem global
    // vazaria salário de terceiros e só fará sentido com papel de administrador.
    [HttpGet]
    public IActionResult ObterProprioUsuario()
    {
        int? id = IdDoToken();

        if (id is null)
            return Forbid();

        Usuario? usuario = _usuarioService.ObterPorId(id.Value);

        if (usuario is null)
            return UsuarioNaoEncontrado();

        return Ok(MapearUsuario(usuario));
    }

    // GET: api/usuario/{id}
    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        if (id != IdDoToken())
            return Forbid();

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
        if (id != IdDoToken())
            return Forbid();

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
        if (id != IdDoToken())
            return Forbid();

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
        if (id != IdDoToken())
            return Forbid();

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
        if (id != IdDoToken())
            return Forbid();

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

    // POST: api/usuario/login — emite o JWT consumido pelo SPA (ADR-010).
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeRateLimit.Login)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        Usuario? usuario = _usuarioService.Autenticar(request.Login, request.Senha);

        if (usuario is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Login ou senha incorretos.");

        if (!usuario.ContaVerificada)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "A conta ainda não foi verificada.");

        TokenAcesso acesso = _authService.GerarToken(usuario);

        return Ok(new LoginResponse
        {
            Mensagem = "Login realizado com sucesso!",
            Token = acesso.Token,
            ExpiraEm = acesso.ExpiraEm,
            Usuario = MapearUsuario(usuario),
        });
    }

    // POST: api/usuario/{id}/verificar — usado pelo fluxo de cadastro, ainda sem sessão.
    [HttpPost("{id}/verificar")]
    [AllowAnonymous]
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

    // Identidade vem do token, nunca do id da URL (arch E1.2, item 4).
    private int? IdDoToken() =>
        int.TryParse(User.FindFirstValue(ClaimsPortfy.Identificador), out int id) ? id : null;
}
