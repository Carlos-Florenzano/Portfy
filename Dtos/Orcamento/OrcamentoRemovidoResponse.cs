namespace Portfy.Dtos.Orcamento;

/// Confirmação do DELETE api/orcamento/{id}.
public sealed class OrcamentoRemovidoResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public int Id { get; init; }
    public string NomeCategoria { get; init; } = string.Empty;
}
