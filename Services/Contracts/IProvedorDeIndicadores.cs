using Portfy.Dtos.Mercado;

namespace Portfy.Services.Contracts;

/// Contrato padronizado de indicador macroeconômico, atendido pelo Banco
/// Central (SGS). É fonte oficial e única: não existe fonte alternativa.
public interface IProvedorDeIndicadores
{
    OrigemDeMercado Origem { get; }

    /// Devolve nulo quando a série existe mas não tem valor publicado.
    Task<IndicadorResponse?> ObterIndicadorAsync(
        IndicadorEconomico indicador,
        CancellationToken cancellationToken);
}
