using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// CRUD e autenticação de usuários sobre o SQLite via AppDbContext, dono único
/// do estado (arch §9). Registrado como Scoped: a instância acompanha a requisição
/// e o DbContext é descartado no fim dela.
///
/// As operações são síncronas porque o contrato público (IUsuarioService e as
/// actions dos controllers) é síncrono e é preservado como requisito: a suíte de
/// 61 testes chama estes métodos diretamente, e tornar tudo assíncrono exigiria
/// alterar controller, contrato e testes de uma só vez.
public sealed class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _db;

    public UsuarioService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // Seam da suíte de testes: mesmo serviço, banco SQLite em memória isolado
    // (sem arquivo físico e sem coleção C# paralela).
    public UsuarioService() : this(AppDbContextEmMemoria.Criar())
    {
    }

    public bool LoginJaExiste(string login) => _db.Usuarios.Any(u => u.Login == login);

    public Usuario Cadastrar(string nome, decimal salarioMensal, string login, string senha)
    {
        // Id 0 = sem identidade: a chave é gerada pelo banco (arch §9.4).
        var usuario = new Usuario(nome, salarioMensal, login, senha);

        _db.Usuarios.Add(usuario);
        _db.SaveChanges();

        return usuario;
    }

    public IReadOnlyCollection<Usuario> ObterTodos() =>
        _db.Usuarios.AsNoTracking().ToList();

    public Usuario? ObterPorId(int id) => _db.Usuarios.Find(id);

    public Usuario? AtualizarDados(int id, string nome, decimal salarioMensal)
    {
        Usuario? usuario = _db.Usuarios.Find(id);

        if (usuario is null)
            return null;

        // O domínio valida e altera; a persistência só confirma o estado final.
        usuario.AtualizarNome(nome);
        usuario.AtualizarSalario(salarioMensal);

        _db.SaveChanges();

        return usuario;
    }

    public Usuario? AtualizarSalario(int id, decimal novoSalario)
    {
        Usuario? usuario = _db.Usuarios.Find(id);

        if (usuario is null)
            return null;

        usuario.AtualizarSalario(novoSalario);

        _db.SaveChanges();

        return usuario;
    }

    public Usuario? AlterarSenha(int id, string senhaAtual, string novaSenha)
    {
        Usuario? usuario = _db.Usuarios.Find(id);

        if (usuario is null)
            return null;

        usuario.AlterarSenha(senhaAtual, novaSenha);

        _db.SaveChanges();

        return usuario;
    }

    public Usuario? Excluir(int id)
    {
        Usuario? usuario = _db.Usuarios.Find(id);

        if (usuario is null)
            return null;

        _db.Usuarios.Remove(usuario);
        _db.SaveChanges();

        return usuario;
    }

    public Usuario? Autenticar(string login, string senha)
    {
        // Comparação do login é feita no SQL com COLLATE NOCASE (ver UsuarioConfiguration).
        Usuario? usuario = _db.Usuarios
            .AsNoTracking()
            .FirstOrDefault(u => u.Login == login);

        return usuario is not null && usuario.VerificarSenha(senha) ? usuario : null;
    }

    public Usuario? VerificarConta(int id)
    {
        Usuario? usuario = _db.Usuarios.Find(id);

        if (usuario is null)
            return null;

        usuario.VerificarConta();

        _db.SaveChanges();

        return usuario;
    }
}
