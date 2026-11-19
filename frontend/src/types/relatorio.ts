// Espelho do contrato JSON do relatório consolidado (arch §10, item 7).
// O período filtra apenas os fluxos (aportes); saldos e posições são o retrato
// atual do estado persistido, como o backend devolve.

export interface AporteDoRelatorio {
    id: number;
    dataAporte: string;
    valorAportado: number;
}

export interface OrcamentoDoRelatorio {
    id: number;
    nomeCategoria: string;
    limiteDefinido: number;
    valorGastoAtual: number;
    saldoRestante: number;
    percentualDoSalario: number;
    estourado: boolean;
    ehFixo: boolean;
}

export interface PosicaoDoRelatorio {
    ticker: string;
    nome: string;
    tipo: string;
    quantidade: number;
    precoMedio: number;
    precoAtual: number;
    valorInvestido: number;
    valorAtual: number;
    resultado: number;
    rentabilidadePercentual: number;
}

export interface Relatorio {
    nomeUsuario: string;
    inicio: string | null;
    fim: string | null;
    salarioMensal: number;
    saldoDisponivel: number;
    patrimonioTotal: number;
    totalInvestido: number;
    totalAportado: number;
    quantidadeAportes: number;
    totalOrcado: number;
    totalGasto: number;
    saldoOrcamentos: number;
    rendaDisponivel: number;
    aportes: AporteDoRelatorio[];
    posicoes: PosicaoDoRelatorio[];
    orcamentos: OrcamentoDoRelatorio[];
}
