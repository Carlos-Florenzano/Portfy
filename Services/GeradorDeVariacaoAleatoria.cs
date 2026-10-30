using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Implementação de produção de IGeradorDeVariacao.
/// O domínio recebe apenas o percentual já sorteado, permanecendo determinístico.
public sealed class GeradorDeVariacaoAleatoria : IGeradorDeVariacao
{
    public decimal SortearPercentual(decimal percentualMinimo, decimal percentualMaximo)
    {
        if (percentualMinimo > percentualMaximo)
            throw new ArgumentException(
                "O percentual mínimo não pode ser maior que o máximo.",
                nameof(percentualMinimo));

        double amplitude = (double)(percentualMaximo - percentualMinimo);
        decimal sorteado = (decimal)(Random.Shared.NextDouble() * amplitude) + percentualMinimo;

        return Math.Round(sorteado, 2, MidpointRounding.AwayFromZero);
    }
}
