using System;

namespace PortifyModulo1;

public class Orcamento
{
    // Nome da categoria de gasto (ex: Alimentação, Transporte, Lazer, Moradia).
    public string NomeCategoria { get; private set; }

    // Valor máximo definido para aquela categoria de gasto.
    public decimal LimiteDefinido { get; private set; }

    // Valor total que já foi gasto dentro da categoria.
    public decimal ValorGastoAtual { get; private set; }

    // Porcentagem do salário mensal destinada à categoria.
    public decimal PercentualDoSalario { get; private set; }

    // Construtor responsável por criar um orçamento com categoria, limite e percentual definidos.
    public Orcamento(string nomeCategoria, decimal limiteDefinido, decimal percentualDoSalario)
    {
        if (string.IsNullOrWhiteSpace(nomeCategoria))
            throw new ArgumentException("O nome da categoria não pode ser vazio.", nameof(nomeCategoria));

        if (limiteDefinido < 0)
            throw new ArgumentOutOfRangeException(nameof(limiteDefinido), "O limite definido não pode ser negativo.");

        if (!TemNoMaximoDuasCasasDecimais(limiteDefinido))
            throw new ArgumentException("O limite definido não pode ter mais de duas casas decimais.", nameof(limiteDefinido));

        if (percentualDoSalario < 0)
            throw new ArgumentOutOfRangeException(nameof(percentualDoSalario), "O percentual do salário não pode ser negativo.");    

        NomeCategoria = nomeCategoria.Trim();
        LimiteDefinido = limiteDefinido;
        PercentualDoSalario = percentualDoSalario;
        ValorGastoAtual = 0;
    }

    // Adiciona uma nova despesa ao orçamento.
    public void AdicionarDespesa(decimal valor, bool confirmarValorExcepcional = false)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor da despesa deve ser maior que zero.");

        if (!TemNoMaximoDuasCasasDecimais(valor))
            throw new ArgumentException("O valor da despesa não pode ter mais de duas casas decimais.", nameof(valor));

        // Trava de sanidade: despesas únicas acima de 3x o limite exigem confirmação explícita
        const decimal MULTIPLICADOR_ALERTA = 3.0m;
        if (valor > LimiteDefinido * MULTIPLICADOR_ALERTA && !confirmarValorExcepcional)
        {
            throw new InvalidOperationException(
                $"O valor {valor:C} é muito superior ao limite da categoria ({LimiteDefinido:C}). " +
                "Se o valor estiver correto, confirme a operação enviando 'true' para confirmarValorExcepcional."
            );
        }

        ValorGastoAtual += valor;
    }

    // Verifica se o valor gasto ultrapassou o limite definido.
    public bool ValidarEstouroOrcamento() => ValorGastoAtual > LimiteDefinido;

    // Retorna o valor restante disponível no orçamento.
    public decimal ObterSaldoRestante() => LimiteDefinido - ValorGastoAtual;

    // Método auxiliar para validar se o valor decimal tem no máximo 2 casas decimais
    private static bool TemNoMaximoDuasCasasDecimais(decimal valor)
    {
        return decimal.Remainder(valor * 100, 1) == 0;
    }
}
