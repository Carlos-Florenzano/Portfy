using System.Security.Cryptography; // NOVO: necessário para proteger e verificar a senha

namespace Portfy.Models;

// Responsabilidade da Classe:
// Gestão da identidade do usuário, entrada de renda
// e autenticação da conta.
public class Usuario
{
    // Id não muda após a criação (get-only)
    public int Id { get; }

    // Propriedades com private set para integridade dos dados
    public string Nome { get; private set; }
    public decimal SalarioMensal { get; private set; }
    public DateTime DataRecebimento { get; private set; }

    // NOVO: login utilizado para acessar o sistema
    public string Login { get; private set; } = string.Empty;
// NOVO: Login do usuário. Fica vazio nos usuários criados pelos construtores antigos.

public string SenhaHash { get; private set; } = string.Empty;
// NOVO: Senha armazenada como hash. Fica vazia nos usuários antigos.

public bool ContaVerificada { get; private set; }
// NOVO: Indica se a conta já foi verificada.


    // Construtor Simplificado
    // Mantido para não quebrar as partes do projeto
    // que já utilizam Usuario(nome, salario).
    public Usuario(string nome, decimal salarioMensal)
        : this(1, nome, salarioMensal, DateTime.Now)
    {
    }


    // NOVO: construtor utilizado para cadastrar
    // um usuário com login e senha.
    public Usuario(
        string nome,
        decimal salarioMensal,
        string login,
        string senha)
        : this(
            1,
            nome,
            salarioMensal,
            DateTime.Now,
            login,
            senha)
    {
    }


    // Construtor Completo ORIGINAL
    public Usuario(
        int id,
        string nome,
        decimal salarioMensal,
        DateTime dataRecebimento)
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

        // Reutiliza a validação do método no construtor
        AtualizarSalario(salarioMensal);
    }


    // NOVO: construtor completo para cadastro
    // com login e senha.
    public Usuario(
        int id,
        string nome,
        decimal salarioMensal,
        DateTime dataRecebimento,
        string login,
        string senha)
    {
        if (id <= 0)
            throw new ArgumentException(
                "O ID do usuário deve ser um valor positivo.",
                nameof(id));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException(
                "O nome do usuário não pode ser vazio.",
                nameof(nome));

        // NOVO: validação do login
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException(
                "O login do usuário não pode ser vazio.",
                nameof(login));

        // NOVO: validação da senha
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException(
                "A senha do usuário não pode ser vazia.",
                nameof(senha));

        Id = id;
        Nome = nome;
        DataRecebimento = dataRecebimento;

        // NOVO: salva o login
        Login = login;

        // NOVO: transforma a senha em hash antes de armazenar
        SenhaHash = CriarHashSenha(senha);

        // NOVO: toda conta começa como não verificada
        ContaVerificada = false;

        // Reutiliza a validação do método no construtor
        AtualizarSalario(salarioMensal);
    }


    // MÉTODO ORIGINAL
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


    // MÉTODO ORIGINAL
    public decimal CalcularRendaDisponivel(decimal totalDespesas)
    {
        return SalarioMensal - totalDespesas;
    }


    // NOVO: cria uma versão protegida da senha.
    // A senha original não é armazenada diretamente.
    private string CriarHashSenha(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            100000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }


    // NOVO: verifica se a senha digitada
    // corresponde à senha cadastrada.
    public bool VerificarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(SenhaHash))
            return false;

        string[] partes = SenhaHash.Split(':');

        if (partes.Length != 2)
            return false;

        byte[] salt = Convert.FromBase64String(partes[0]);
        byte[] hashArmazenado = Convert.FromBase64String(partes[1]);

        byte[] hashInformado = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            100000,
            HashAlgorithmName.SHA256,
            32);

        return CryptographicOperations.FixedTimeEquals(
            hashArmazenado,
            hashInformado);
    }


    // NOVO: verifica o login e a senha
    // informados pelo usuário.
    public bool VerificarLogin(string login, string senha)
    {
        return Login.Equals(
                   login,
                   StringComparison.OrdinalIgnoreCase)
               && VerificarSenha(senha);
    }


    // NOVO: confirma que a conta foi verificada.
    public void VerificarConta()
    {
        ContaVerificada = true;
    }
}