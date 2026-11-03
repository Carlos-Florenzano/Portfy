// Formatação de exibição e classes de formulário compartilhadas.
// Fica fora de `ui.tsx` porque aquele arquivo só exporta componentes (Fast Refresh).

export function formatarMoeda(valor: number): string {
    return (valor || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function formatarData(valor: string): string {
    const data = new Date(valor);
    return Number.isNaN(data.getTime()) ? '—' : data.toLocaleDateString('pt-BR');
}

export function formatarPercentual(valor: number): string {
    return `${(valor || 0).toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`;
}

// Todas as telas usam a mesma altura de campo e o mesmo foco visível.
export const classesDeEntrada =
    'w-full rounded-xl border border-portfy-borda bg-portfy-fundo px-3.5 py-2.5 text-sm text-portfy-texto ' +
    'placeholder:text-portfy-suave/60 outline-none transition ' +
    'focus:border-portfy-acento focus:ring-2 focus:ring-portfy-acento/30';

export const classesDeRotulo = 'mb-1.5 block text-xs font-semibold uppercase tracking-wide text-portfy-suave';
