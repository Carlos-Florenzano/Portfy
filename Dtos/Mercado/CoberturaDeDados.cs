namespace Portfy.Dtos.Mercado;

/// Quanto do contrato padronizado a fonte realmente entregou.
///
/// A ordem dos membros é intencional: `Parcial` é o valor padrão do enum, então
/// um provedor que esqueça de declarar a cobertura nunca superestima o dado.
public enum CoberturaDeDados
{
    /// Só parte dos campos foi publicada pela fonte (ex.: múltiplos da BRAPI).
    Parcial,

    /// A fonte entrega o conjunto completo do contrato (ex.: perfil da FMP).
    Completa,
}
