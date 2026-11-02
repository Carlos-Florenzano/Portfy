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

export const aporteService = {
    criar: async (valor: number, carteiraId?: number): Promise<AporteCriado> => {
        const resposta = await api.post<AporteCriado>('/aporte', { valor, carteiraId });
        return resposta.data;
    },
};
