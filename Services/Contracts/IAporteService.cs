using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Resultado da criação de um aporte: o aporte persistido e o saldo da carteira
/// depois da operação, para o SPA atualizar os cartões sem uma segunda chamada.
public sealed record AporteRealizado(
    AporteSimulado Aporte,
    decimal SaldoDisponivel,
    decimal PatrimonioTotal);

/// Histórico de aportes de um usuário, já filtrado por período e totalizado.
public sealed record HistoricoDeAportes(
    IReadOnlyCollection<AporteSimulado> Aportes,
    decimal Total);

/// Contrato injetado de criação e histórico de aportes. O aporte pertence à carteira
/// do usuário autenticado e soma ao saldo na mesma transação (arch §9.1).
public interface IAporteService
{
    AporteRealizado Criar(int usuarioId, decimal valorAportado, int? carteiraId = null);

    /// Aportes do usuário em ordem decrescente de data. `inicio` e `fim` são
    /// inclusivos e opcionais: omitidos, o histórico é completo.
    HistoricoDeAportes ObterHistorico(int usuarioId, DateOnly? inicio, DateOnly? fim);
}
