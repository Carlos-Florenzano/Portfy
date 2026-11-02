using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado do orçamento: a leitura e a escrita do gasto passam pelo serviço,
/// nunca por um campo estático do controller. Todo orçamento é de um usuário (arch §9.1).
public interface IOrcamentoService
{
    Orcamento ObterOrcamento(int usuarioId);

    void AdicionarDespesa(int usuarioId, decimal valor);
}
