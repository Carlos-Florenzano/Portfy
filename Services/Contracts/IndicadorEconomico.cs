namespace Portfy.Services.Contracts;

/// Indicadores macroeconômicos suportados. Cada valor corresponde a uma série
/// oficial do SGS; código numérico, descrição e unidade ficam no provedor do
/// Banco Central, que é a fonte primária desse dado.
public enum IndicadorEconomico
{
    Ipca,
    Selic,
    Cdi,
    Dolar,
}
