using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado do orçamento: a leitura e a escrita do gasto passam pelo serviço,
/// nunca por um campo estático do controller.
public interface IOrcamentoService
{
    Orcamento ObterOrcamento();

    void AdicionarDespesa(decimal valor);
}
