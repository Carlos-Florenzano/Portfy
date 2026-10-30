// Integração HTTP do recurso usuário: telas nunca chamam `api` diretamente.
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

export interface CredenciaisLogin {
  login: string;
  senha: string;
}

// Confirmação devolvida pelo cadastro e pelo login do backend.
export interface UsuarioConfirmado {
  mensagem: string;
  id: number;
  nome: string;
  login: string;
  contaVerificada: boolean;
}

export interface ContaVerificada {
  mensagem: string;
  id: number;
  contaVerificada: boolean;
}

export interface UsuarioAtualizado {
  mensagem: string;
  id: number;
  nome: string;
  salarioMensal: number;
}

export interface MensagemResposta {
  mensagem: string;
}

export const usuarioService = {
  // CREATE (Cadastro)
  cadastrar: async (dados: CadastroUsuarioDto): Promise<UsuarioConfirmado> => {
    const resposta = await api.post<UsuarioConfirmado>('/usuario/cadastro', dados);
    return resposta.data;
  },

  // Activação da conta
  verificarConta: async (id: number): Promise<ContaVerificada> => {
    const resposta = await api.post<ContaVerificada>(`/usuario/${id}/verificar`);
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
  atualizar: async (id: number, dados: AtualizarUsuarioDto): Promise<UsuarioAtualizado> => {
    const resposta = await api.put<UsuarioAtualizado>(`/usuario/${id}`, dados);
    return resposta.data;
  },

  // DELETE (Remover conta)
  excluir: async (id: number): Promise<MensagemResposta> => {
    const resposta = await api.delete<MensagemResposta>(`/usuario/${id}`);
    return resposta.data;
  },

  // Login
  login: async (credenciais: CredenciaisLogin): Promise<UsuarioConfirmado> => {
    const resposta = await api.post<UsuarioConfirmado>('/usuario/login', credenciais);
    return resposta.data;
  }
};
