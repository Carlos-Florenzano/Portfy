// Navegação principal do painel. O menu é dado aqui (uma fonte só): cada item
// aponta para uma seção, e a seção ativa é recebida por props.
import { BarChart3, Banknote, LayoutDashboard, LogOut, Trash2, TrendingUp, Wallet } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type { Usuario } from '../services/UsuarioService';

export type SecaoDoPainel = 'painel' | 'salario' | 'investimentos' | 'aportes' | 'relatorios';

interface ItemDoMenu {
    chave: SecaoDoPainel;
    rotulo: string;
    Icone: LucideIcon;
}

const MENU: ItemDoMenu[] = [
    { chave: 'painel', rotulo: 'Dashboard', Icone: LayoutDashboard },
    { chave: 'salario', rotulo: 'Salário e Orçamento', Icone: Wallet },
    { chave: 'investimentos', rotulo: 'Investimentos', Icone: TrendingUp },
    { chave: 'aportes', rotulo: 'Aportes', Icone: Banknote },
    { chave: 'relatorios', rotulo: 'Relatórios', Icone: BarChart3 },
];

export function BarraLateral({
    secao,
    onSelecionar,
    usuario,
    onSair,
    onExcluirConta,
}: {
    secao: SecaoDoPainel;
    onSelecionar: (secao: SecaoDoPainel) => void;
    usuario: Usuario;
    onSair: () => void;
    onExcluirConta: () => void;
}) {
    return (
        <aside className="sticky top-0 flex h-screen w-64 shrink-0 flex-col border-r border-portfy-borda bg-[#070707] px-4 py-7">
            <div className="mb-10 px-2 text-2xl font-extrabold tracking-widest text-white">
                <span className="text-portfy-acento">PORT</span>FY
            </div>

            <nav className="flex flex-col gap-1" aria-label="Seções do painel">
                {MENU.map(({ chave, rotulo, Icone }) => {
                    const ativa = chave === secao;

                    return (
                        <button
                            key={chave}
                            type="button"
                            onClick={() => onSelecionar(chave)}
                            aria-current={ativa ? 'page' : undefined}
                            className={
                                'flex w-full items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium transition ' +
                                (ativa
                                    ? 'bg-portfy-acento text-white'
                                    : 'text-portfy-suave hover:bg-portfy-superficie-alta hover:text-portfy-texto')
                            }
                        >
                            <Icone size={18} aria-hidden />
                            {rotulo}
                        </button>
                    );
                })}
            </nav>

            <div className="mt-auto">
                <div className="mb-5 flex items-center gap-3 rounded-xl border border-portfy-borda bg-portfy-superficie px-3 py-3">
                    <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-portfy-acento text-sm font-bold text-white">
                        {usuario.nome ? usuario.nome.charAt(0).toUpperCase() : 'U'}
                    </span>
                    <span className="min-w-0">
                        <span className="block truncate text-sm font-semibold text-portfy-texto">{usuario.nome}</span>
                        <span className="block truncate text-xs text-portfy-suave">{usuario.login}</span>
                    </span>
                </div>

                {/*
                  Espaçamento ampliado entre as duas ações de saída: "Excluir Conta"
                  é irreversível e estava encostada em "Sair", o que favorecia clique
                  acidental. gap-6 mantém 24px entre elas.
                */}
                <div className="flex flex-col gap-6 border-t border-portfy-borda pt-5">
                    <button
                        type="button"
                        onClick={onSair}
                        className="flex w-full items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium text-portfy-suave transition hover:bg-portfy-superficie-alta hover:text-portfy-texto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-portfy-borda"
                    >
                        <LogOut size={18} aria-hidden />
                        Sair
                    </button>

                    <button
                        type="button"
                        onClick={onExcluirConta}
                        className="flex w-full items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium text-portfy-negativo transition hover:bg-portfy-negativo/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-portfy-negativo/40"
                    >
                        <Trash2 size={18} aria-hidden />
                        Excluir Conta
                    </button>
                </div>
            </div>
        </aside>
    );
}
