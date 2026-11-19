// Visão geral do painel. Todos os números vêm do relatório consolidado do
// backend (GET /relatorio): a tela não soma nem recalcula valor financeiro.
import { useEffect, useState } from 'react';
import {
    ArrowDownToLine,
    CircleDollarSign,
    LineChart,
    PiggyBank,
    Plus,
    TrendingUp,
    Wallet,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { mensagemDeErro } from '../services/api';
import { relatorioService } from '../services/RelatorioService';
import type { Relatorio } from '../types/relatorio';
import { IndicadoresDeMercado } from '../components/IndicadoresDeMercado';
import { GraficoDeOrcamentos } from '../components/GraficoDeOrcamentos';
import { Aviso, Botao, Cartao, EstadoVazio, TituloDaSecao } from '../components/ui';
import { formatarData, formatarMoeda, formatarPercentual } from '../components/formatadores';
import type { SecaoDoPainel } from '../components/BarraLateral';

function CartaoDeIndicador({
    Icone,
    rotulo,
    valor,
    detalhe,
    cor,
}: {
    Icone: LucideIcon;
    rotulo: string;
    valor: string;
    detalhe: string;
    cor: string;
}) {
    return (
        <Cartao className="flex min-h-36 flex-col justify-between">
            <div className="flex items-start justify-between gap-3">
                <span className="text-sm font-semibold text-portfy-suave">{rotulo}</span>
                <span className={`flex size-9 items-center justify-center rounded-xl ${cor}`}>
                    <Icone size={18} aria-hidden />
                </span>
            </div>
            <div>
                <h3 className="mt-4 text-2xl font-bold text-portfy-texto">{valor}</h3>
                <p className="mt-1 text-xs text-portfy-suave">{detalhe}</p>
            </div>
        </Cartao>
    );
}

export function Painel({ onNavegar }: { onNavegar: (secao: SecaoDoPainel) => void }) {
    const [relatorio, setRelatorio] = useState<Relatorio | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState('');

    useEffect(() => {
        let montado = true;

        async function carregar() {
            try {
                const dados = await relatorioService.obter();

                if (!montado) return;

                setRelatorio(dados);
                setErro('');
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
    }, []);

    if (carregando) {
        return <p className="text-sm text-portfy-suave">Carregando seus dados financeiros...</p>;
    }

    if (erro !== '') {
        return <Aviso tipo="erro" texto={erro} />;
    }

    if (!relatorio) return null;

    const orcamentos = relatorio.orcamentos;
    const posicoes = relatorio.posicoes;
    const ultimosAportes = relatorio.aportes.slice(0, 5);

    return (
        <div className="flex flex-col gap-7">
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-4">
                <CartaoDeIndicador
                    Icone={Wallet}
                    rotulo="Salário mensal"
                    valor={formatarMoeda(relatorio.salarioMensal)}
                    detalhe="Renda mensal cadastrada"
                    cor="bg-portfy-acento/15 text-portfy-acento-claro"
                />
                <CartaoDeIndicador
                    Icone={CircleDollarSign}
                    rotulo="Saldo disponível"
                    valor={formatarMoeda(relatorio.saldoDisponivel)}
                    detalhe="Disponível para novos aportes"
                    cor="bg-portfy-positivo/15 text-portfy-positivo"
                />
                <CartaoDeIndicador
                    Icone={PiggyBank}
                    rotulo="Patrimônio total"
                    valor={formatarMoeda(relatorio.patrimonioTotal)}
                    detalhe="Saldo mais posições da carteira"
                    cor="bg-portfy-acento/15 text-portfy-acento-claro"
                />
                <CartaoDeIndicador
                    Icone={TrendingUp}
                    rotulo="Total investido"
                    valor={formatarMoeda(relatorio.totalInvestido)}
                    detalhe={`${posicoes.length} posição(ões) na carteira`}
                    cor="bg-portfy-positivo/15 text-portfy-positivo"
                />
            </div>

            <IndicadoresDeMercado />

            <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
                <Cartao>
                    <TituloDaSecao
                        titulo="Orçamentos"
                        descricao="Distribuição do teto definido por categoria"
                        acao={
                            <Botao variante="secundario" onClick={() => onNavegar('salario')}>
                                <Plus size={16} aria-hidden />
                                Novo
                            </Botao>
                        }
                    />

                    {orcamentos.length === 0 ? (
                        <EstadoVazio
                            Icone={PiggyBank}
                            titulo="Nenhum orçamento cadastrado"
                            descricao="Defina um teto por categoria para acompanhar seus gastos mês a mês."
                            acao={
                                <Botao onClick={() => onNavegar('salario')}>
                                    <Plus size={16} aria-hidden />
                                    Cadastrar primeiro orçamento
                                </Botao>
                            }
                        />
                    ) : (
                        <GraficoDeOrcamentos orcamentos={orcamentos} />
                    )}
                </Cartao>

                <Cartao>
                    <TituloDaSecao
                        titulo="Investimentos"
                        descricao="Posições atuais da carteira"
                        acao={
                            <Botao variante="secundario" onClick={() => onNavegar('investimentos')}>
                                <LineChart size={16} aria-hidden />
                                Ver carteira
                            </Botao>
                        }
                    />

                    {posicoes.length === 0 ? (
                        <EstadoVazio
                            Icone={LineChart}
                            titulo="Nenhum investimento carregado"
                            descricao="Compre seu primeiro ativo para acompanhar rentabilidade e patrimônio."
                            acao={
                                <Botao onClick={() => onNavegar('investimentos')}>
                                    <Plus size={16} aria-hidden />
                                    Escolher ativo
                                </Botao>
                            }
                        />
                    ) : (
                        <ul className="divide-y divide-portfy-borda">
                            {posicoes.map((posicao) => (
                                <li key={posicao.ticker} className="flex items-center justify-between gap-3 py-3">
                                    <span className="min-w-0">
                                        <strong className="block text-sm text-portfy-texto">{posicao.ticker}</strong>
                                        <small className="block truncate text-xs text-portfy-suave">
                                            {posicao.quantidade} cota(s) · {posicao.nome}
                                        </small>
                                    </span>
                                    <span className="text-right">
                                        <span className="block text-sm font-semibold text-portfy-texto">
                                            {formatarMoeda(posicao.valorAtual)}
                                        </span>
                                        <small
                                            className={
                                                posicao.resultado >= 0
                                                    ? 'block text-xs text-portfy-positivo'
                                                    : 'block text-xs text-portfy-negativo'
                                            }
                                        >
                                            {formatarPercentual(posicao.rentabilidadePercentual)}
                                        </small>
                                    </span>
                                </li>
                            ))}
                        </ul>
                    )}
                </Cartao>
            </div>

            <Cartao>
                <TituloDaSecao
                    titulo="Últimos aportes"
                    descricao="Histórico dos aportes realizados"
                    acao={
                        <Botao variante="secundario" onClick={() => onNavegar('aportes')}>
                            <ArrowDownToLine size={16} aria-hidden />
                            Ver histórico
                        </Botao>
                    }
                />

                {ultimosAportes.length === 0 ? (
                    <EstadoVazio
                        Icone={ArrowDownToLine}
                        titulo="Nenhum aporte registrado"
                        descricao="Registre um aporte para começar a mover saldo para a carteira."
                        acao={
                            <Botao onClick={() => onNavegar('aportes')}>
                                <Plus size={16} aria-hidden />
                                Registrar aporte
                            </Botao>
                        }
                    />
                ) : (
                    <ul className="divide-y divide-portfy-borda">
                        {ultimosAportes.map((aporte) => (
                            <li key={aporte.id} className="flex items-center justify-between gap-3 py-3">
                                <span className="text-sm text-portfy-suave">{formatarData(aporte.dataAporte)}</span>
                                <span className="text-sm font-semibold text-portfy-positivo">
                                    {formatarMoeda(aporte.valorAportado)}
                                </span>
                            </li>
                        ))}
                    </ul>
                )}
            </Cartao>
        </div>
    );
}
