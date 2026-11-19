// Blocos visuais reutilizados pelas telas do painel: cartão, título de seção,
// estado vazio, aviso e botão. Só apresentação — nenhum serviço de dados aqui.
import type { ButtonHTMLAttributes, ReactNode } from 'react';
import type { LucideIcon } from 'lucide-react';

export function Cartao({ children, className = '' }: { children: ReactNode; className?: string }) {
    return (
        <section className={`rounded-2xl border border-portfy-borda bg-portfy-superficie p-6 ${className}`}>
            {children}
        </section>
    );
}

export function TituloDaSecao({
    titulo,
    descricao,
    acao,
}: {
    titulo: string;
    descricao: string;
    acao?: ReactNode;
}) {
    return (
        <div className="mb-5 flex items-start justify-between gap-4">
            <div>
                <h2 className="text-lg font-bold text-portfy-texto">{titulo}</h2>
                <p className="mt-1 text-sm text-portfy-suave">{descricao}</p>
            </div>
            {acao}
        </div>
    );
}

// Estado vazio: ícone vetorial + convite à ação, nunca um texto de placeholder.
export function EstadoVazio({
    Icone,
    titulo,
    descricao,
    acao,
}: {
    Icone: LucideIcon;
    titulo: string;
    descricao: string;
    acao?: ReactNode;
}) {
    return (
        <div className="flex min-h-44 flex-col items-center justify-center rounded-xl border border-dashed border-portfy-borda px-6 py-10 text-center">
            <span className="mb-3 flex size-12 items-center justify-center rounded-full bg-portfy-superficie-alta text-portfy-suave">
                <Icone size={22} aria-hidden />
            </span>
            <h3 className="text-sm font-semibold text-portfy-texto">{titulo}</h3>
            <p className="mt-1.5 max-w-sm text-sm text-portfy-suave">{descricao}</p>
            {acao ? <div className="mt-4">{acao}</div> : null}
        </div>
    );
}

export function Aviso({ tipo, texto }: { tipo: 'sucesso' | 'erro'; texto: string }) {
    const cor =
        tipo === 'sucesso'
            ? 'border-portfy-positivo/40 bg-portfy-positivo/10 text-portfy-positivo'
            : 'border-portfy-negativo/40 bg-portfy-negativo/10 text-portfy-negativo';

    return (
        <p role="status" className={`rounded-xl border px-4 py-3 text-sm font-medium ${cor}`}>
            {texto}
        </p>
    );
}

type VarianteDeBotao = 'primario' | 'secundario' | 'perigo';

const variantes: Record<VarianteDeBotao, string> = {
    primario:
        'bg-portfy-acento text-white hover:bg-portfy-acento/85 focus-visible:ring-portfy-acento/40',
    secundario:
        'border border-portfy-borda text-portfy-texto hover:bg-portfy-superficie-alta focus-visible:ring-portfy-borda',
    perigo:
        'border border-portfy-negativo/40 text-portfy-negativo hover:bg-portfy-negativo/10 focus-visible:ring-portfy-negativo/30',
};

export function Botao({
    variante = 'primario',
    className = '',
    children,
    ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variante?: VarianteDeBotao }) {
    return (
        <button
            {...props}
            className={
                'inline-flex items-center justify-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold ' +
                'transition focus-visible:outline-none focus-visible:ring-2 disabled:cursor-not-allowed ' +
                `disabled:opacity-50 ${variantes[variante]} ${className}`
            }
        >
            {children}
        </button>
    );
}
