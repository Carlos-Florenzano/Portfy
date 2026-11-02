// Integração HTTP da carteira: o dono é sempre o usuário do token.
// Nenhuma tela chama `api` diretamente.
import { api } from './api';
import type { Carteira, OperacaoCarteira } from '../types/carteira';

export const carteiraService = {
    obter: async (): Promise<Carteira> => {
        const resposta = await api.get<Carteira>('/carteira');
        return resposta.data;
    },

    comprar: async (ticker: string, quantidade: number): Promise<OperacaoCarteira> => {
        const resposta = await api.post<OperacaoCarteira>('/carteira/compras', {
            ticker,
            quantidade,
        });
        return resposta.data;
    },

    vender: async (ticker: string, quantidade: number): Promise<OperacaoCarteira> => {
        const resposta = await api.post<OperacaoCarteira>('/carteira/vendas', {
            ticker,
            quantidade,
        });
        return resposta.data;
    },
};
