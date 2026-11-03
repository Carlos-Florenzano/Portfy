// Estrutura do painel: dados do usuário logado, navegação entre módulos e o
// contêiner visual comum. Cada módulo é uma página própria em `src/pages`.
import { useEffect, useState } from 'react';
import { RotateCcw } from 'lucide-react';
import { mensagemDeErro } from './services/api';
import { lerSessao, limparSessao } from './services/sessao';
import { usuarioService } from './services/UsuarioService';
import type { Usuario } from './services/UsuarioService';
import { BarraLateral } from './components/BarraLateral';
import type { SecaoDoPainel } from './components/BarraLateral';
import { PerfilDoUsuario } from './components/PerfilDoUsuario';
import { Botao } from './components/ui';
import { Painel } from './pages/Painel';
import { SalarioOrcamento } from './pages/SalarioOrcamento';
import { Investimentos } from './pages/Investimentos';
import { Aportes } from './pages/Aportes';
import { Relatorios } from './pages/Relatorios';

// A identidade vem do token guardado no login; a API valida o token a cada chamada.
function idDoUsuarioLogado(): number | null {
    return lerSessao()?.usuario.id ?? null;
}

const TITULOS: Record<SecaoDoPainel, { rotulo: string; descricao: string }> = {
    painel: { rotulo: 'Painel financeiro', descricao: 'Acompanhe sua vida financeira em um só lugar.' },
    salario: { rotulo: 'Salário e orçamento', descricao: 'Renda mensal e tetos de gasto por categoria.' },
    investimentos: { rotulo: 'Investimentos', descricao: 'Catálogo de ativos e posições da carteira.' },
    aportes: { rotulo: 'Aportes', descricao: 'Depósitos na carteira e histórico por período.' },
    relatorios: { rotulo: 'Relatórios', descricao: 'Consolidação financeira por período e exportação.' },
};

function Dashboard() {
    const [usuario, setUsuario] = useState<Usuario | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState('');
    const [secao, setSecao] = useState<SecaoDoPainel>('painel');

    // Cada incremento refaz a busca do usuário; o botão "Tentar novamente" só
    // troca o gatilho. Mesmo padrão de IndicadoresDeMercado: o efeito é o único
    // dono da carga e nunca chama setState de forma síncrona.
    const [tentativa, setTentativa] = useState(0);

    useEffect(() => {
        let montado = true;

        async function carregar() {
            try {
                setCarregando(true);
                setErro('');

                const id = idDoUsuarioLogado();

                if (id === null) {
                    setErro('Nenhum usuário logado. Faça login novamente.');
                    return;
                }

                const dados = await usuarioService.obterPorId(id);

                if (montado) setUsuario(dados);
            } catch (falha: unknown) {
                if (montado) setErro(mensagemDeErro(falha));
            } finally {
                if (montado) setCarregando(false);
            }
        }

        void carregar();

        return () => {
            montado = false;
        };
    }, [tentativa]);

    async function encerrarConta() {
        if (!usuario) return;

        const confirmou = window.confirm('Atenção: deseja realmente excluir sua conta? Esta ação não pode ser desfeita.');
        if (!confirmou) return;

        try {
            await usuarioService.excluir(usuario.id);
            limparSessao();
            window.location.reload();
        } catch (falha: unknown) {
            setErro(mensagemDeErro(falha));
        }
    }

    function sair() {
        limparSessao();
        window.location.reload();
    }

    if (carregando) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-portfy-fundo text-sm text-portfy-suave">
                Carregando Portfy...
            </div>
        );
    }

    if (erro !== '' || !usuario) {
        return (
            <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-portfy-fundo px-6 text-center">
                <h2 className="text-xl font-bold text-portfy-texto">Erro ao carregar o Portfy</h2>
                <p className="max-w-md text-sm text-portfy-suave">{erro}</p>
                <Botao onClick={() => setTentativa((valor) => valor + 1)}>
                    <RotateCcw size={16} aria-hidden />
                    Tentar novamente
                </Botao>
            </div>
        );
    }

    const titulo = TITULOS[secao];

    return (
        <div className="flex min-h-screen bg-portfy-fundo text-portfy-texto">
            <BarraLateral
                secao={secao}
                onSelecionar={setSecao}
                usuario={usuario}
                onSair={sair}
                onExcluirConta={() => void encerrarConta()}
            />

            <main className="flex min-w-0 flex-1 flex-col gap-7 px-6 py-8 lg:px-10">
                <header className="flex flex-col justify-between gap-6 lg:flex-row lg:items-start">
                    <div>
                        <p className="mb-1.5 text-xs font-bold uppercase tracking-widest text-portfy-acento-claro">
                            {titulo.rotulo}
                        </p>
                        <h1 className="text-3xl font-bold text-white">
                            {secao === 'painel' ? `Olá, ${usuario.nome}` : titulo.rotulo}
                        </h1>
                        <p className="mt-1.5 text-sm text-portfy-suave">{titulo.descricao}</p>
                    </div>

                    <PerfilDoUsuario usuario={usuario} onAtualizado={setUsuario} />
                </header>

                {secao === 'painel' && <Painel onNavegar={setSecao} />}
                {secao === 'salario' && (
                    <SalarioOrcamento
                        usuario={usuario}
                        onSalarioAtualizado={(salarioMensal) => setUsuario({ ...usuario, salarioMensal })}
                    />
                )}
                {secao === 'investimentos' && <Investimentos />}
                {secao === 'aportes' && <Aportes />}
                {secao === 'relatorios' && <Relatorios />}
            </main>
        </div>
    );
}

export default Dashboard;
