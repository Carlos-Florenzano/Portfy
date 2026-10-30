using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// CRUD e autenticação de usuários sobre uma coleção em memória.
/// Registrado como Singleton porque o estado ainda não vive no SQLite (dívida P0-a);
/// o lock protege a coleção, que é compartilhada entre requisições concorrentes.
public sealed class UsuarioService : IUsuarioService
{
    private readonly object _lock = new();
    private readonly List<Usuario> _usuarios = [];
    private int _proximoId = 1;

    public bool LoginJaExiste(string login)
    {
        lock (_lock)
        {
            return _usuarios.Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        }
    }

    public Usuario Cadastrar(string nome, decimal salarioMensal, string login, string senha)
    {
        lock (_lock)
        {
            var usuario = new Usuario(_proximoId, nome, salarioMensal, DateTime.Now, login, senha);

            _usuarios.Add(usuario);
            _proximoId++;

            return usuario;
        }
    }

    public IReadOnlyCollection<Usuario> ObterTodos()
    {
        lock (_lock)
        {
            return _usuarios.ToArray();
        }
    }

    public Usuario? ObterPorId(int id)
    {
        lock (_lock)
        {
            return Localizar(id);
        }
    }

    public Usuario? AtualizarDados(int id, string nome, decimal salarioMensal)
    {
        lock (_lock)
        {
            Usuario? usuario = Localizar(id);

            if (usuario is null)
                return null;

            usuario.AtualizarNome(nome);
            usuario.AtualizarSalario(salarioMensal);

            return usuario;
        }
    }

    public Usuario? AtualizarSalario(int id, decimal novoSalario)
    {
        lock (_lock)
        {
            Usuario? usuario = Localizar(id);

            if (usuario is null)
                return null;

            usuario.AtualizarSalario(novoSalario);

            return usuario;
        }
    }

    public Usuario? AlterarSenha(int id, string senhaAtual, string novaSenha)
    {
        lock (_lock)
        {
            Usuario? usuario = Localizar(id);

            if (usuario is null)
                return null;

            usuario.AlterarSenha(senhaAtual, novaSenha);

            return usuario;
        }
    }

    public Usuario? Excluir(int id)
    {
        lock (_lock)
        {
            Usuario? usuario = Localizar(id);

            if (usuario is null)
                return null;

            _usuarios.Remove(usuario);

            return usuario;
        }
    }

    public Usuario? Autenticar(string login, string senha)
    {
        lock (_lock)
        {
            Usuario? usuario = _usuarios.FirstOrDefault(u =>
                u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));

            return usuario is not null && usuario.VerificarSenha(senha) ? usuario : null;
        }
    }

    public Usuario? VerificarConta(int id)
    {
        lock (_lock)
        {
            Usuario? usuario = Localizar(id);

            if (usuario is null)
                return null;

            usuario.VerificarConta();

            return usuario;
        }
    }

    private Usuario? Localizar(int id) => _usuarios.FirstOrDefault(u => u.Id == id);
}
