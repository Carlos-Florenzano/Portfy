using Portfy.Dtos.Mercado;

namespace Portfy.Services.Contracts;

/// Fronteira única de dados de mercado para o restante da aplicação: escolhe a
/// fonte por responsabilidade, aplica cache e faz fallback quando a preferida
/// falha. O consumidor não conhece FMP, BRAPI nem SGS.
public interface IMercadoService
{
    Task<CotacaoResponse> ObterCotacaoAsync(string ticker, CancellationToken cancellationToken);

    Task<FundamentoResponse> ObterFundamentosAsync(string ticker, CancellationToken cancellationToken);

    Task<IndicadoresResponse> ObterIndicadoresAsync(CancellationToken cancellationToken);
}
