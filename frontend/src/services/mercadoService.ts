// Integração HTTP dos dados de mercado. Uma tela nunca importa axios: sempre passa aqui.
import { api } from './api';
import type { RespostaDeIndicadores } from '../types/mercado';

export const mercadoService = {
  indicadores: async (): Promise<RespostaDeIndicadores> => {
    const resposta = await api.get<RespostaDeIndicadores>('/mercado/indicadores');
    return resposta.data;
  },
};
