using Portfy.Dtos.Mercado;

namespace Portfy.Services.Contracts;

/// Contrato padronizado de dados fundamentalistas (FMP: perfil da empresa e
/// múltiplos). A BRAPI não cobre fundamentos profundos, então não há fallback
/// para este contrato — decisão registrada na ADR-020.
public interface IProvedorDeFundamentos
{
    OrigemDeMercado Origem { get; }

    bool EstaConfigurado { get; }

    /// Devolve nulo quando a fonte respondeu que o ticker não existe.
    Task<FundamentoResponse?> ObterFundamentosAsync(string ticker, CancellationToken cancellationToken);
}
