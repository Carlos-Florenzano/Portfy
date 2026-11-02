namespace Portfy.Dtos.Carteira;

/// Resposta de POST api/carteira/compras e POST api/carteira/vendas.
public sealed class OperacaoCarteiraResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public string Ticker { get; init; } = string.Empty;
    public int Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }
    public decimal ValorTotal { get; init; }
    public decimal SaldoDisponivel { get; init; }
    public decimal PatrimonioTotal { get; init; }
}
