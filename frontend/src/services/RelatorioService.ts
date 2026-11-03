// Integração HTTP do relatório consolidado: a tela não soma valores financeiros,
// ela apenas exibe o que o backend já calculou (arch §6.2).
import { api } from './api';
import type { Relatorio } from '../types/relatorio';

function parametrosDePeriodo(inicio?: string, fim?: string) {
    return {
        inicio: inicio ? inicio : undefined,
        fim: fim ? fim : undefined,
    };
}

function nomeDoArquivo(inicio?: string, fim?: string): string {
    const recorte = !inicio && !fim ? 'completo' : `${inicio || 'inicio'}_${fim || 'fim'}`;
    return `portfy-relatorio-${recorte}.csv`;
}

export const relatorioService = {
    obter: async (inicio?: string, fim?: string): Promise<Relatorio> => {
        const resposta = await api.get<Relatorio>('/relatorio', {
            params: parametrosDePeriodo(inicio, fim),
        });
        return resposta.data;
    },

    // Mesmo recorte em CSV. O serviço devolve o arquivo; baixá-lo é papel da tela.
    exportarCsv: async (inicio?: string, fim?: string): Promise<{ conteudo: Blob; nome: string }> => {
        const resposta = await api.get<Blob>('/relatorio/csv', {
            params: parametrosDePeriodo(inicio, fim),
            responseType: 'blob',
        });

        return { conteudo: resposta.data, nome: nomeDoArquivo(inicio, fim) };
    },
};
