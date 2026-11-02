// Espelho do contrato JSON da carteira (arch §10, item 7).
// Nenhuma conta financeira é feita aqui: os valores vêm prontos do backend.

export interface AtivoDaPosicao {
    ticker: string;
    nome: string;
    tipo: string;
    precoAtual: number;
    rentabilidadeAcumulada: number;
}

export interface PosicaoDaCarteira {
    id: number;
    ativo: AtivoDaPosicao;
    quantidade: number;
    precoMedio: number;
}

export interface AporteDaCarteira {
    id: number;
    dataAporte: string;
    valorAportado: number;
    origemSalarioId: number;
    carteiraId: number;
}

export interface Carteira {
    id: number;
    nome: string;
    patrimonioTotal: number;
    saldoDisponivel: number;
    posicoes: PosicaoDaCarteira[];
    aportes: AporteDaCarteira[];
}

export interface OperacaoCarteira {
    mensagem: string;
    ticker: string;
    quantidade: number;
    precoUnitario: number;
    valorTotal: number;
    saldoDisponivel: number;
    patrimonioTotal: number;
}
