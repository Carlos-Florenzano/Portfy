using System.Text.Json.Serialization;

namespace Portfy.Models;

public class Orcamento
{
    // Chave primária gerada pelo banco (arch §9.4). Fora do JSON para preservar
    // exatamente o contrato HTTP atual de api/orcamento.
    [JsonIgnore]
    public int Id { get; private set; }

    // Dono do orçamento. Cada usuário tem as suas próprias categorias (arch §9.1).
    [JsonIgnore]
    public int UsuarioId { get; private set; }

    // Nome da categoria de gasto (ex: Alimentação, Transporte, Lazer, Moradia).
    public string NomeCategoria { get; private set; } = string.Empty;

    // Valor máximo definido para aquela categoria de gasto.
    public decimal LimiteDefinido { get; private set; }

    // Valor total que já foi gasto dentro da categoria.
    public decimal ValorGastoAtual { get; private set; }

    // Porcentagem do salário mensal destinada à categoria.
    public decimal PercentualDoSalario { get; private set; }

    // Indica despesa fixa recorrente. O fluxo atual cria o orçamento com false.
    public bool EhFixo { get; private set; }

    // Construtor exigido pelo EF Core (arch §13.1); o domínio usa o construtor validado.
    protected Orcamento()
    {
    }

    // Construtor responsável por criar um orçamento com dono, categoria, limite e percentual definidos.
    public Orcamento(
        int usuarioId,
        string nomeCategoria,
        decimal limiteDefinido,
        decimal percentualDoSalario,
        bool ehFixo = false)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(usuarioId));

        if (string.IsNullOrWhiteSpace(nomeCategoria))
            throw new ArgumentException("O nome da categoria não pode ser vazio.", nameof(nomeCategoria));

        if (limiteDefinido < 0)
            throw new ArgumentOutOfRangeException(nameof(limiteDefinido), "O limite definido não pode ser negativo.");

        if (percentualDoSalario < 0)
            throw new ArgumentOutOfRangeException(nameof(percentualDoSalario), "O percentual do salário não pode ser negativo.");

        UsuarioId = usuarioId;
        NomeCategoria = nomeCategoria;
        LimiteDefinido = limiteDefinido;
        PercentualDoSalario = percentualDoSalario;
        ValorGastoAtual = 0;
        EhFixo = ehFixo;
    }

    // Adiciona uma nova despesa ao orçamento.
    public void AdicionarDespesa(decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor da despesa deve ser maior que zero.");

        ValorGastoAtual += valor;
    }

    // Verifica se o valor gasto ultrapassou o limite definido.
    public bool ValidarEstouroOrcamento() => ValorGastoAtual > LimiteDefinido;

    // Retorna o valor restante disponível no orçamento.
    public decimal ObterSaldoRestante() => LimiteDefinido - ValorGastoAtual;
}
