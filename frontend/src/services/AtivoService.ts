// Integração HTTP do recurso ativo. O catálogo é leitura pública; o cadastro
// altera dado compartilhado por todos os usuários e exige o token da sessão.
import { api } from './api';

export interface Ativo {
  ticker: string;
  nome: string;
  tipo: string;
  precoAtual: number;
  rentabilidadeAcumulada: number;
}

export interface NovoAtivo {
  ticker: string;
  nome: string;
  tipo: string;
  precoAtual: number;
}

export const ativoService = {
  obterTodos: async (): Promise<Ativo[]> => {
    const resposta = await api.get<Ativo[]>('/ativo');
    return resposta.data;
  },

  criar: async (dados: NovoAtivo): Promise<Ativo> => {
    const resposta = await api.post<Ativo>('/ativo', dados);
    return resposta.data;
  }
};
