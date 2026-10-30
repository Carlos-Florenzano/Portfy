using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado do CRUD de usuários e da autenticação.
/// O controller decide o status HTTP; este serviço decide o que é permitido.
public interface IUsuarioService
{
    bool LoginJaExiste(string login);

    Usuario Cadastrar(string nome, decimal salarioMensal, string login, string senha);

    IReadOnlyCollection<Usuario> ObterTodos();

    Usuario? ObterPorId(int id);

    Usuario? AtualizarDados(int id, string nome, decimal salarioMensal);

    Usuario? AtualizarSalario(int id, decimal novoSalario);

    Usuario? AlterarSenha(int id, string senhaAtual, string novaSenha);

    Usuario? Excluir(int id);

    Usuario? Autenticar(string login, string senha);

    Usuario? VerificarConta(int id);
}
