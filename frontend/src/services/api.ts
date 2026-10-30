// Ponto único de acesso HTTP: a instância axios e a tradução de erro vivem aqui, nunca nas telas.
import axios from 'axios';

const MENSAGEM_PADRAO = 'Não foi possível concluir a operação. Tente novamente.';
const MENSAGEM_SEM_CONEXAO =
    'Não foi possível conectar ao servidor. Verifique se a API está em execução.';

// Erro já traduzido para exibição: a tela só lê a mensagem, nunca o corpo da resposta.
export class ErroRequisicao extends Error {
    constructor(mensagem: string) {
        super(mensagem);
        this.name = 'ErroRequisicao';
    }
}

interface CorpoDeErro {
    detail?: unknown;
    mensagem?: unknown;
    Mensagem?: unknown;
}

function extrairMensagemDoCorpo(corpo: unknown): string | null {
    if (typeof corpo !== 'object' || corpo === null) {
        return null;
    }

    const { detail, mensagem, Mensagem } = corpo as CorpoDeErro;

    for (const candidata of [detail, mensagem, Mensagem]) {
        if (typeof candidata === 'string' && candidata.trim().length > 0) {
            return candidata;
        }
    }

    return null;
}

function traduzirErro(erro: unknown): ErroRequisicao {
    if (!axios.isAxiosError(erro)) {
        return new ErroRequisicao(erro instanceof Error ? erro.message : MENSAGEM_PADRAO);
    }

    if (!erro.response) {
        return new ErroRequisicao(MENSAGEM_SEM_CONEXAO);
    }

    return new ErroRequisicao(extrairMensagemDoCorpo(erro.response.data) ?? MENSAGEM_PADRAO);
}

// Instância apontando para a porta da API C# (.NET)
export const api = axios.create({
    baseURL: 'http://localhost:5000/api',
});

// Interceptor de resposta: toda falha chega ao consumidor como ErroRequisicao,
// com a mensagem do ProblemDetails (RFC 7807) ou do envelope legado já resolvida.
api.interceptors.response.use(
    (resposta) => resposta,
    (erro: unknown) => Promise.reject(traduzirErro(erro)),
);

// Mensagem exibida em tela para qualquer erro que chegue ao catch de uma tela.
export function mensagemDeErro(erro: unknown): string {
    return erro instanceof ErroRequisicao ? erro.message : MENSAGEM_PADRAO;
}
