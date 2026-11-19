namespace Portfy.Dtos.Mercado;

/// Origem de um dado de mercado. É a mesma identidade usada para rotear a
/// requisição e para registrar de onde o dado veio (`Fonte` das respostas),
/// então consumidor e rastreabilidade nunca divergem.
public enum OrigemDeMercado
{
    Brapi,
    Fmp,
    BcbSgs,
}
