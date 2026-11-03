// Integração HTTP do orçamento do usuário autenticado.
import { api } from './api';
import type {
    AlteracaoOrcamento,
    DespesaAdicionada,
    ListaOrcamentos,
    NovoOrcamento,
    Orcamento,
    OrcamentoRemovido,
} from '../types/orcamento';

export const orcamentoService = {
    // Leitura do orçamento padrão (contrato anterior, preservado).
    obter: async (): Promise<Orcamento> => {
        const resposta = await api.get<Orcamento>('/orcamento');
        return resposta.data;
    },

    // Todas as categorias com os totais consolidados.
    obterTodos: async (): Promise<ListaOrcamentos> => {
        const resposta = await api.get<ListaOrcamentos>('/orcamento/todos');
        return resposta.data;
    },

    criar: async (dados: NovoOrcamento): Promise<Orcamento> => {
        const resposta = await api.post<Orcamento>('/orcamento', dados);
        return resposta.data;
    },

    atualizar: async (id: number, dados: AlteracaoOrcamento): Promise<Orcamento> => {
        const resposta = await api.put<Orcamento>(`/orcamento/${id}`, dados);
        return resposta.data;
    },

    excluir: async (id: number): Promise<OrcamentoRemovido> => {
        const resposta = await api.delete<OrcamentoRemovido>(`/orcamento/${id}`);
        return resposta.data;
    },

    adicionarDespesa: async (valor: number): Promise<DespesaAdicionada> => {
        const resposta = await api.post<DespesaAdicionada>('/orcamento/despesa', { valor });
        return resposta.data;
    },

    // Despesa em uma categoria específica: o Id vem da listagem.
    adicionarDespesaNaCategoria: async (id: number, valor: number): Promise<DespesaAdicionada> => {
        const resposta = await api.post<DespesaAdicionada>(`/orcamento/${id}/despesa`, { valor });
        return resposta.data;
    },
};
