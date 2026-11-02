// Integração HTTP do orçamento do usuário autenticado.
import { api } from './api';
import type { DespesaAdicionada, Orcamento } from '../types/orcamento';

export const orcamentoService = {
    obter: async (): Promise<Orcamento> => {
        const resposta = await api.get<Orcamento>('/orcamento');
        return resposta.data;
    },

    adicionarDespesa: async (valor: number): Promise<DespesaAdicionada> => {
        const resposta = await api.post<DespesaAdicionada>('/orcamento/despesa', { valor });
        return resposta.data;
    },
};
