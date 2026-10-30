namespace Portfy.Services.Contracts;

/// Isola a aleatoriedade da simulação de preço: o domínio continua determinístico
/// e o teste injeta uma variação fixa.
public interface IGeradorDeVariacao
{
    decimal SortearPercentual(decimal percentualMinimo, decimal percentualMaximo);
}
