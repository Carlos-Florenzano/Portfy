// Criei o UsuarioService em Service do Frontend para integrar o CRUD para o usuário
import { api } from './api';

export interface Usuario {
  id: number;
  nome: string;
  login: string;
  salarioMensal: number;
  dataRecebimento?: string;
  contaVerificada: boolean;
}

export interface CadastroUsuarioDto {
  nome: string;
  salarioMensal: number;
  login: string;
  senha: string;
}

export interface AtualizarUsuarioDto {
  nome: string;
  salarioMensal: number;
}

export const usuarioService = {
  // CREATE (Cadastro)
  cadastrar: async (dados: CadastroUsuarioDto) => {
    const resposta = await api.post('/usuario/cadastro', dados);
    return resposta.data;
  },

  // Activação da conta
  verificarConta: async (id: number) => {
    const resposta = await api.post(`/usuario/${id}/verificar`);
    return resposta.data;
  },

  // READ (Obter utilizador por ID)
  obterPorId: async (id: number): Promise<Usuario> => {
    const resposta = await api.get<Usuario>(`/usuario/${id}`);
    return resposta.data;
  },

  // READ (Listar todos)
  obterTodos: async (): Promise<Usuario[]> => {
    const resposta = await api.get<Usuario[]>('/usuario');
    return resposta.data;
  },

  // UPDATE (Nome e Salário)
  atualizar: async (id: number, dados: AtualizarUsuarioDto) => {
    const resposta = await api.put(`/usuario/${id}`, dados);
    return resposta.data;
  },

  // DELETE (Remover conta)
  excluir: async (id: number) => {
    const resposta = await api.delete(`/usuario/${id}`);
    return resposta.data;
  },

  // Login
  login: async (credenciais: { login: string; senha: string }) => {
    const resposta = await api.post('/usuario/login', credenciais);
    return resposta.data;
  }
};