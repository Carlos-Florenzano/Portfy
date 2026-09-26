using System;

namespace GestaoSalarioOrcamento;

// Responsabilidade da Classe: Gestão da identidade do usuário e entrada de renda.
public class Usuario
{
    public int Id { get; }

    public string Nome { get; private set; }
    public decimal SalarioMensal { get; private set; }
    public DateTime DataRecebimento { get; private set; }

    // Construtor simplificado
    public Usuario(string nome, decimal salarioMensal)
        : this(1, nome, salarioMensal, DateTime.Now)
    {
    }

    // Construtor completo
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

        AtualizarSalario(salarioMensal);
    }

    public void AtualizarSalario(decimal novoSalario)
    {
        if (novoSalario < 0)
            throw new ArgumentOutOfRangeException(
                nameof(novoSalario),
                "O salário mensal não pode ser negativo.");

        SalarioMensal = novoSalario;
    }

    public decimal CalcularRendaDisponivel(decimal totalDespesas)
    {
        if (totalDespesas < 0)
            throw new ArgumentOutOfRangeException(
                nameof(totalDespesas),
                "O total de despesas não pode ser negativo.");

        return SalarioMensal - totalDespesas;
    }
}
