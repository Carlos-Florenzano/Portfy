// Integração HTTP do recurso ativo.
import { api } from './api';

export interface Ativo {
  ticker: string;
  nome: string;
  tipo: string;
  precoAtual: number;
  rentabilidadeAcumulada: number;
}

export const ativoService = {
  obterTodos: async (): Promise<Ativo[]> => {
    const resposta = await api.get<Ativo[]>('/ativo');
    return resposta.data;
  }
};
