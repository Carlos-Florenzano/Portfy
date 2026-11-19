namespace Portfy.Dtos.Ativo;

/// Entrada do POST api/ativo: cadastra um novo ativo no catálogo simulado.
/// O catálogo é dado de mercado compartilhado, não dado financeiro de um usuário.
public sealed class CriarAtivoRequest
{
    public string Ticker { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;

    /// Um dos valores de TipoAtivo: Acao, Fii, RendaFixa, Cripto, Etf.
    public string Tipo { get; init; } = string.Empty;

    public decimal PrecoAtual { get; init; }
}
