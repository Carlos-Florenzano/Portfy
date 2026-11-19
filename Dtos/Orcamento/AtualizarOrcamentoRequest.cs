namespace Portfy.Dtos.Orcamento;

/// Entrada do PUT api/orcamento/{id}: redefine o teto de uma categoria existente.
/// `NomeCategoria` é opcional para preservar o contrato anterior: quando omitido,
/// o nome atual é mantido; quando informado, a categoria é renomeada.
public sealed class AtualizarOrcamentoRequest
{
    public string? NomeCategoria { get; init; }
    public decimal LimiteDefinido { get; init; }
    public decimal PercentualDoSalario { get; init; }
    public bool EhFixo { get; init; }
}
