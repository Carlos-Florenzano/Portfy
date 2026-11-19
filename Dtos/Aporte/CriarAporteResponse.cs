namespace Portfy.Dtos.Aporte;

/// Resposta do POST api/aporte. O saldo e o patrimônio vêm da mesma transação
/// que gravou o aporte, então o SPA não precisa refazer o GET da carteira.
public sealed class CriarAporteResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public AporteResponse Aporte { get; init; } = new();
    public decimal SaldoDisponivel { get; init; }
    public decimal PatrimonioTotal { get; init; }
}
