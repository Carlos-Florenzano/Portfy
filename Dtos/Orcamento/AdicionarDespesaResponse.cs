namespace Portfy.Dtos.Orcamento;

/// Resposta do POST api/orcamento/despesa.
public sealed class AdicionarDespesaResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public string NomeCategoria { get; init; } = string.Empty;
    public decimal ValorGastoAtual { get; init; }
    public decimal SaldoRestante { get; init; }
    public bool Estourado { get; init; }
}
