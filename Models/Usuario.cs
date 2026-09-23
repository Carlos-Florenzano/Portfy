using System;

namespace GestaoSalarioOrcamento;

// Responsabilidade da Classe: Gestão da identidade do usuário e entrada de renda.
public class Usuario
{
    // Id não muda após a criação (get-only)
    public int Id { get; }

    // Propriedades com private set para integridade dos dados
    public string Nome { get; private set; }
    public decimal SalarioMensal { get; private set; }
    public DateTime DataRecebimento { get; private set; }

    // Construtor Simplificado (Permite instanciação limpa no Program.cs / Dashboard.cs)
    public Usuario(string nome, decimal salarioMensal)
        : this(1, nome, salarioMensal, DateTime.Now)
    {
    }

    // Construtor Completo
    public Usuario(int id, string nome, decimal salarioMensal, DateTime dataRecebimento)
    {
        if (id <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(id));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do usuário não pode ser vazio.", nameof(nome));

        Id = id;
        Nome = nome;
        DataRecebimento = dataRecebimento;

        // Reutiliza a validação do método no construtor
        AtualizarSalario(salarioMensal);
    }

    public void AtualizarSalario(decimal novoSalario)
    {
        if (novoSalario < 0)
        {
            throw new ArgumentException("O salário mensal não pode ser negativo.", nameof(novoSalario));
        }

        SalarioMensal = novoSalario;
    }

    public decimal CalcularRendaDisponivel(decimal totalDespesas)
    {
        return SalarioMensal - totalDespesas;
    }
}