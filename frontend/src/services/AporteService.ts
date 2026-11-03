// Integração HTTP do aporte: o backend usa a carteira padrão do usuário
// quando `carteiraId` é omitido e soma o valor ao saldo na mesma transação.
import { api } from './api';
import type { AporteDaCarteira } from '../types/carteira';

export interface AporteCriado {
    mensagem: string;
    aporte: AporteDaCarteira;
    saldoDisponivel: number;
    patrimonioTotal: number;
}

// GET /aporte: recorte do histórico, com total e quantidade já somados no backend.
export interface HistoricoDeAportes {
    total: number;
    quantidade: number;
    aportes: AporteDaCarteira[];
}

function parametrosDePeriodo(inicio?: string, fim?: string) {
    // Datas vazias não viajam na query: o backend trata ausência como "sem recorte".
    return {
        inicio: inicio ? inicio : undefined,
        fim: fim ? fim : undefined,
    };
}

export const aporteService = {
    criar: async (valor: number, carteiraId?: number): Promise<AporteCriado> => {
        const resposta = await api.post<AporteCriado>('/aporte', { valor, carteiraId });
        return resposta.data;
    },

    historico: async (inicio?: string, fim?: string): Promise<HistoricoDeAportes> => {
        const resposta = await api.get<HistoricoDeAportes>('/aporte', {
            params: parametrosDePeriodo(inicio, fim),
        });
        return resposta.data;
    },
};
