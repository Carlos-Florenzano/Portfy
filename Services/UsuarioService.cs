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
/// testes chama estes métodos diretamente, e tornar tudo assíncrono exigiria
/// alterar controller, contrato e testes de uma só vez.
public sealed class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _db;
    private readonly ISenhaHasher _senhaHasher;

    public UsuarioService(AppDbContext db, ISenhaHasher senhaHasher)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _senhaHasher = senhaHasher ?? throw new ArgumentNullException(nameof(senhaHasher));
    }

    // Seam da suíte de testes: mesmo serviço, banco SQLite em memória isolado
    // (sem arquivo físico e sem coleção C# paralela).
    public UsuarioService() : this(AppDbContextEmMemoria.Criar(), new SenhaHasher())
    {
    }

    public bool LoginJaExiste(string login) => _db.Usuarios.Any(u => u.Login == login);

    public Usuario Cadastrar(string nome, decimal salarioMensal, string login, string senha)
    {
        // O hash é calculado antes de tocar no domínio: o modelo nunca vê a
        // senha em texto puro (arch §13, item 5).
        var usuario = new Usuario(nome, salarioMensal, login, _senhaHasher.CriarHash(senha));

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

        // Comparar a senha atual é regra de aplicação, não do modelo.
        if (!_senhaHasher.Verificar(senhaAtual, usuario.SenhaHash))
            throw new ArgumentException("A senha atual informada está incorreta.");

        usuario.AlterarSenhaHash(_senhaHasher.CriarHash(novaSenha));

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

        return usuario is not null && _senhaHasher.Verificar(senha, usuario.SenhaHash)
            ? usuario
            : null;
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
