namespace Portfy.Models;

// Responsabilidade da Classe:
// Gestão da identidade do usuário, entrada de renda e autenticação da conta.
// Hash de senha NÃO mora aqui (arch §13, item 5): o modelo recebe `SenhaHash`
// pronto do UsuarioService, que delega ao ISenhaHasher.
public class Usuario
{
    // Id não muda após a criação. O setter privado existe apenas para que a
    // persistência grave a chave autoincrementada; o domínio não expõe escrita.
    public int Id { get; private set; }

    public string Nome { get; private set; }
    public decimal SalarioMensal { get; private set; }
    public DateTime DataRecebimento { get; private set; }

    // Login utilizado para acessar o sistema.
    public string Login { get; private set; } = string.Empty;

    // Senha já protegida. Fica vazia apenas nos usuários criados pelos
    // construtores antigos, que não têm login.
    public string SenhaHash { get; private set; } = string.Empty;

    public bool ContaVerificada { get; private set; }

    // Construtor simplificado, mantido para não quebrar as partes do projeto
    // que já utilizam Usuario(nome, salario).
    public Usuario(string nome, decimal salarioMensal)
        : this(1, nome, salarioMensal, DateTime.Now)
    {
    }

    // Construtor de cadastro: recebe a senha JÁ em hash.
    // Id = 0 significa "ainda sem identidade": a chave é atribuída pela persistência.
    public Usuario(string nome, decimal salarioMensal, string login, string senhaHash)
        : this(0, nome, salarioMensal, DateTime.Now, login, senhaHash)
    {
    }

    // Construtor exigido pelo EF Core para materializar um usuário já persistido.
    private Usuario()
    {
        Nome = string.Empty;
    }

    // Construtor completo original (sem login).
    public Usuario(int id, string nome, decimal salarioMensal, DateTime dataRecebimento)
    {
        if (id <= 0)
            throw new ArgumentException(
                "O ID do usuário deve ser um valor positivo.",
                nameof(id));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException(
                "O nome do usuário não pode ser vazio.",
                nameof(nome));

        Id = id;
        Nome = nome;
        DataRecebimento = dataRecebimento;

        // Reutiliza a validação do método no construtor.
        AtualizarSalario(salarioMensal);
    }

    // Construtor completo para cadastro com login e senha já protegida.
    public Usuario(
        int id,
        string nome,
        decimal salarioMensal,
        DateTime dataRecebimento,
        string login,
        string senhaHash)
    {
        // Id 0 é reservado ao usuário ainda não persistido (chave gerada pelo banco).
        if (id < 0)
            throw new ArgumentException(
                "O ID do usuário deve ser um valor positivo.",
                nameof(id));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException(
                "O nome do usuário não pode ser vazio.",
                nameof(nome));

        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException(
                "O login do usuário não pode ser vazio.",
                nameof(login));

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException(
                "O hash da senha do usuário não pode ser vazio.",
                nameof(senhaHash));

        Id = id;
        Nome = nome;
        DataRecebimento = dataRecebimento;
        Login = login;
        SenhaHash = senhaHash;

        // Toda conta começa como não verificada.
        ContaVerificada = false;

        // Reutiliza a validação do método no construtor.
        AtualizarSalario(salarioMensal);
    }

    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
        {
            throw new ArgumentException("O nome do usuário não pode ser vazio.", nameof(novoNome));
        }

        Nome = novoNome;
    }

    // A troca de senha chega aqui já verificada e já em hash pelo UsuarioService:
    // comparar senha exige o ISenhaHasher, que não pertence ao domínio.
    public void AlterarSenhaHash(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
        {
            throw new ArgumentException("O hash da nova senha não pode ser vazio.", nameof(novoHash));
        }

        SenhaHash = novoHash;
    }

    public void AtualizarSalario(decimal novoSalario)
    {
        if (novoSalario < 0)
        {
            throw new ArgumentException(
                "O salário mensal não pode ser negativo.",
                nameof(novoSalario));
        }

        SalarioMensal = novoSalario;
    }

    public decimal CalcularRendaDisponivel(decimal totalDespesas)
    {
        return SalarioMensal - totalDespesas;
    }

    public void VerificarConta()
    {
        ContaVerificada = true;
    }
}
