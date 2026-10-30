using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Fonte única do estado da carteira dentro do processo. A instância vive no container
/// de DI (Singleton) enquanto a persistência em SQLite não entra (dívida P0-a);
/// quando o AppDbContext assumir, esta classe passa a ser Scoped e o saldo sai da memória.
public sealed class CarteiraService : ICarteiraService
{
    private const decimal SaldoInicialPadrao = 10000m;

    private readonly CarteiraSimulada _carteira = new(SaldoInicialPadrao);

    public CarteiraSimulada ObterCarteira() => _carteira;
}
