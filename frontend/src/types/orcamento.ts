// Espelho do contrato JSON do orçamento (arch §10, item 7).

export interface Orcamento {
    nomeCategoria: string;
    limiteDefinido: number;
    valorGastoAtual: number;
    percentualDoSalario: number;
    saldoRestante: number;
    estourado: boolean;
    ehFixo: boolean;
}

export interface DespesaAdicionada {
    mensagem: string;
    nomeCategoria: string;
    valorGastoAtual: number;
    saldoRestante: number;
    estourado: boolean;
}
