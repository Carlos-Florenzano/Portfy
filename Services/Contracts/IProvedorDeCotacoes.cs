using Portfy.Dtos.Mercado;

namespace Portfy.Services.Contracts;

/// Contrato padronizado de cotação. Implementado pela BRAPI (ativos da B3) e
/// pela FMP (ativos globais); o orquestrador define a ordem e o fallback.
public interface IProvedorDeCotacoes
{
    OrigemDeMercado Origem { get; }

    /// Falso quando falta a credencial da fonte. Nesse caso o provedor não é
    /// chamado, e a ausência conta como indisponibilidade — nunca como ativo
    /// inexistente.
    bool EstaConfigurado { get; }

    /// Devolve nulo quando a fonte respondeu que o ticker não existe.
    Task<CotacaoResponse?> ObterCotacaoAsync(string ticker, CancellationToken cancellationToken);
}
