using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado do orçamento: a leitura e a escrita do gasto passam pelo serviço,
/// nunca por um campo estático do controller. Todo orçamento é de um usuário (arch §9.1).
public interface IOrcamentoService
{
    /// Orçamento padrão do usuário (primeira categoria cadastrada), criado sob demanda.
    Orcamento ObterOrcamento(int usuarioId);

    /// Todas as categorias de gasto do usuário, em ordem de cadastro.
    IReadOnlyCollection<Orcamento> ObterTodos(int usuarioId);

    Orcamento? ObterPorId(int usuarioId, int orcamentoId);

    Orcamento Criar(
        int usuarioId,
        string nomeCategoria,
        decimal limiteDefinido,
        decimal percentualDoSalario,
        bool ehFixo);

    Orcamento? Atualizar(
        int usuarioId,
        int orcamentoId,
        decimal limiteDefinido,
        decimal percentualDoSalario,
        bool ehFixo);

    bool Excluir(int usuarioId, int orcamentoId);

    /// Mantido como contrato anterior: soma a despesa ao orçamento padrão.
    void AdicionarDespesa(int usuarioId, decimal valor);

    /// Soma a despesa a uma categoria específica do usuário.
    Orcamento? AdicionarDespesa(int usuarioId, int orcamentoId, decimal valor);
}
