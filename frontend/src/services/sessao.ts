// Sessão do SPA: o token JWT é a credencial; os dados do usuário são cache de UI.
// Nenhuma tela lê o token diretamente — só `api.ts` o envia no cabeçalho.

const CHAVE = "portfy.sessao";

export interface UsuarioSessao {
    id: number;
    nome: string;
    login: string;
}

export interface Sessao {
    token: string;
    expiraEm: string;
    usuario: UsuarioSessao;
}

// Evento disparado quando o backend recusa o token (401). App.tsx volta ao login.
export const EVENTO_SESSAO_EXPIRADA = "portfy:sessao-expirada";

export function salvarSessao(sessao: Sessao): void {
    localStorage.setItem(CHAVE, JSON.stringify(sessao));
}

export function limparSessao(): void {
    localStorage.removeItem(CHAVE);
}

export function lerSessao(): Sessao | null {
    const bruto = localStorage.getItem(CHAVE);

    if (!bruto) return null;

    try {
        const dados: unknown = JSON.parse(bruto);

        if (typeof dados !== "object" || dados === null) return null;

        const { token, expiraEm, usuario } = dados as Partial<Sessao>;

        if (typeof token !== "string" || typeof expiraEm !== "string") return null;
        if (typeof usuario !== "object" || usuario === null) return null;

        const { id, nome, login } = usuario as Partial<UsuarioSessao>;

        if (typeof id !== "number" || typeof nome !== "string" || typeof login !== "string") {
            return null;
        }

        return { token, expiraEm, usuario: { id, nome, login } };
    } catch {
        // Conteúdo corrompido é tratado como ausência de sessão, não como erro de tela.
        return null;
    }
}

export function lerToken(): string | null {
    return lerSessao()?.token ?? null;
}

// Verificação local apenas para decidir a tela inicial; quem valida o token é a API.
export function sessaoValida(): boolean {
    const sessao = lerSessao();

    if (!sessao) return false;

    const expiraEm = Date.parse(sessao.expiraEm);

    return Number.isFinite(expiraEm) && expiraEm > Date.now();
}
