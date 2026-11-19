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

// Confirmação devolvida pelo cadastro do backend.
export interface UsuarioConfirmado {
  mensagem: string;
  id: number;
  nome: string;
  login: string;
  contaVerificada: boolean;
}

// Resposta do login: token JWT + dados do usuário (ADR-010).
export interface LoginRealizado {
  mensagem: string;
  token: string;
  expiraEm: string;
  usuario: Usuario;
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

// Resposta do PUT /usuario/{id}/salario: traz o salário anterior e o novo.
export interface SalarioAtualizado {
  mensagem: string;
  salarioAnterior: number;
  novoSalario: number;
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

  // READ (Obter utilizador por ID) — o id precisa ser o do token
  obterPorId: async (id: number): Promise<Usuario> => {
    const resposta = await api.get<Usuario>(`/usuario/${id}`);
    return resposta.data;
  },

  // UPDATE (Nome e Salário)
  atualizar: async (id: number, dados: AtualizarUsuarioDto): Promise<UsuarioAtualizado> => {
    const resposta = await api.put<UsuarioAtualizado>(`/usuario/${id}`, dados);
    return resposta.data;
  },

  // UPDATE (somente renda): rota dedicada usada pelo módulo Salário e Orçamento.
  atualizarSalario: async (id: number, novoSalario: number): Promise<SalarioAtualizado> => {
    const resposta = await api.put<SalarioAtualizado>(`/usuario/${id}/salario`, { novoSalario });
    return resposta.data;
  },

  // DELETE (Remover conta)
  excluir: async (id: number): Promise<MensagemResposta> => {
    const resposta = await api.delete<MensagemResposta>(`/usuario/${id}`);
    return resposta.data;
  },

  // Login: devolve o token que `api.ts` passa a enviar em toda requisição.
  login: async (credenciais: CredenciaisLogin): Promise<LoginRealizado> => {
    const resposta = await api.post<LoginRealizado>('/usuario/login', credenciais);
    return resposta.data;
  }
};
