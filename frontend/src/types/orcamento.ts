// Espelho do contrato JSON do orçamento (arch §10, item 7).

export interface Orcamento {
    // Identidade estável da categoria: é o Id usado em PUT e DELETE.
    id: number;
    nomeCategoria: string;
    limiteDefinido: number;
    valorGastoAtual: number;
    percentualDoSalario: number;
    saldoRestante: number;
    estourado: boolean;
    ehFixo: boolean;
}

// Resposta do GET /orcamento/todos: os totais chegam calculados do backend.
export interface ListaOrcamentos {
    orcamentos: Orcamento[];
    totalLimiteDefinido: number;
    totalGastoAtual: number;
    totalSaldoRestante: number;
    percentualComprometido: number;
}

export interface NovoOrcamento {
    nomeCategoria: string;
    limiteDefinido: number;
    percentualDoSalario: number;
    ehFixo: boolean;
}

// O PUT aceita o nome junto com teto, percentual e recorrência.
export interface AlteracaoOrcamento {
    nomeCategoria: string;
    limiteDefinido: number;
    percentualDoSalario: number;
    ehFixo: boolean;
}

export interface OrcamentoRemovido {
    mensagem: string;
    id: number;
    nomeCategoria: string;
}

export interface DespesaAdicionada {
    mensagem: string;
    nomeCategoria: string;
    valorGastoAtual: number;
    saldoRestante: number;
    estourado: boolean;
}
