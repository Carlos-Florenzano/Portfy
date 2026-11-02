using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Resultado da criação de um aporte: o aporte persistido e o saldo da carteira
/// depois da operação, para o SPA atualizar os cartões sem uma segunda chamada.
public sealed record AporteRealizado(
    AporteSimulado Aporte,
    decimal SaldoDisponivel,
    decimal PatrimonioTotal);

/// Contrato injetado de criação de aportes. O aporte pertence à carteira do
/// usuário autenticado e soma ao saldo na mesma transação (arch §9.1).
public interface IAporteService
{
    AporteRealizado Criar(int usuarioId, decimal valorAportado, int? carteiraId = null);
}
