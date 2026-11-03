// Módulo "Relatórios": consolidação por período e exportação em CSV.
// O backend entrega os totais; a tela só formata e baixa o arquivo.
import { useCallback, useEffect, useState } from 'react';
import { BarChart3, Download, RefreshCw } from 'lucide-react';
import { mensagemDeErro } from '../services/api';
import { relatorioService } from '../services/RelatorioService';
import type { Relatorio } from '../types/relatorio';
import { Aviso, Botao, Cartao, EstadoVazio, TituloDaSecao } from '../components/ui';
import {
    classesDeEntrada,
    classesDeRotulo,
    formatarData,
    formatarMoeda,
    formatarPercentual,
} from '../components/formatadores';

interface Recado {
    tipo: 'sucesso' | 'erro';
    texto: string;
}

// Download local do blob devolvido pela API (o arquivo é gerado no backend).
function baixarArquivo(conteudo: Blob, nome: string) {
    const url = URL.createObjectURL(conteudo);
    const link = document.createElement('a');

    link.href = url;
    link.download = nome;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
}

function IndicadorDoRelatorio({ rotulo, valor, destaque = false }: { rotulo: string; valor: string; destaque?: boolean }) {
    return (
        <div className="rounded-xl border border-portfy-borda bg-portfy-fundo px-4 py-3">
            <span className="block text-xs font-semibold uppercase tracking-wide text-portfy-suave">{rotulo}</span>
            <strong className={`mt-1 block text-lg font-bold ${destaque ? 'text-portfy-positivo' : 'text-portfy-texto'}`}>
                {valor}
            </strong>
        </div>
    );
}

export function Relatorios() {
    const [relatorio, setRelatorio] = useState<Relatorio | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [baixando, setBaixando] = useState(false);
    const [recado, setRecado] = useState<Recado | null>(null);

    const [inicio, setInicio] = useState('');
    const [fim, setFim] = useState('');

    const carregar = useCallback(async (inicioDoRecorte?: string, fimDoRecorte?: string) => {
        const dados = await relatorioService.obter(inicioDoRecorte, fimDoRecorte);
        setRelatorio(dados);
    }, []);

    useEffect(() => {
        let montado = true;

        async function iniciar() {
            try {
                await carregar();
            } catch (falha: unknown) {
                if (montado) setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
            } finally {
                if (montado) setCarregando(false);
            }
        }

        void iniciar();

        return () => {
            montado = false;
        };
    }, [carregar]);

    async function gerar() {
        try {
            setCarregando(true);
            await carregar(inicio, fim);
            setRecado(null);
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setCarregando(false);
        }
    }

    async function exportar() {
        try {
            setBaixando(true);
            const arquivo = await relatorioService.exportarCsv(inicio, fim);
            baixarArquivo(arquivo.conteudo, arquivo.nome);
            setRecado({ tipo: 'sucesso', texto: `Arquivo ${arquivo.nome} gerado.` });
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setBaixando(false);
        }
    }

    return (
        <div className="flex flex-col gap-7">
            {recado && <Aviso tipo={recado.tipo} texto={recado.texto} />}

            <Cartao>
                <TituloDaSecao
                    titulo="Período"
                    descricao="O recorte filtra os aportes; saldos e posições são o retrato atual."
                />

                <div className="flex flex-wrap items-end gap-3">
                    <div className="min-w-40 flex-1">
                        <label className={classesDeRotulo} htmlFor="relatorio-inicio">
                            De
                        </label>
                        <input
                            id="relatorio-inicio"
                            className={classesDeEntrada}
                            type="date"
                            value={inicio}
                            onChange={(evento) => setInicio(evento.target.value)}
                        />
                    </div>
                    <div className="min-w-40 flex-1">
                        <label className={classesDeRotulo} htmlFor="relatorio-fim">
                            Até
                        </label>
                        <input
                            id="relatorio-fim"
                            className={classesDeEntrada}
                            type="date"
                            value={fim}
                            onChange={(evento) => setFim(evento.target.value)}
                        />
                    </div>

                    <Botao onClick={() => void gerar()} disabled={carregando}>
                        <RefreshCw size={15} aria-hidden />
                        {carregando ? 'Gerando...' : 'Gerar relatório'}
                    </Botao>

                    <Botao variante="secundario" onClick={() => void exportar()} disabled={baixando}>
                        <Download size={15} aria-hidden />
                        {baixando ? 'Exportando...' : 'Exportar CSV'}
                    </Botao>
                </div>
            </Cartao>

            {!relatorio ? (
                <Cartao>
                    <EstadoVazio
                        Icone={BarChart3}
                        titulo="Nenhum relatório carregado"
                        descricao="Escolha um período e gere o relatório consolidado."
                    />
                </Cartao>
            ) : (
                <>
                    <Cartao>
                        <TituloDaSecao
                            titulo={`Resumo de ${relatorio.nomeUsuario}`}
                            descricao={
                                relatorio.inicio || relatorio.fim
                                    ? `Aportes de ${relatorio.inicio ?? 'início'} a ${relatorio.fim ?? 'hoje'}`
                                    : 'Todos os aportes registrados'
                            }
                        />

                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
                            <IndicadorDoRelatorio rotulo="Salário mensal" valor={formatarMoeda(relatorio.salarioMensal)} />
                            <IndicadorDoRelatorio rotulo="Saldo disponível" valor={formatarMoeda(relatorio.saldoDisponivel)} destaque />
                            <IndicadorDoRelatorio rotulo="Patrimônio total" valor={formatarMoeda(relatorio.patrimonioTotal)} />
                            <IndicadorDoRelatorio rotulo="Total investido" valor={formatarMoeda(relatorio.totalInvestido)} />
                            <IndicadorDoRelatorio
                                rotulo={`Aportado (${relatorio.quantidadeAportes})`}
                                valor={formatarMoeda(relatorio.totalAportado)}
                            />
                            <IndicadorDoRelatorio rotulo="Total orçado" valor={formatarMoeda(relatorio.totalOrcado)} />
                            <IndicadorDoRelatorio rotulo="Total gasto" valor={formatarMoeda(relatorio.totalGasto)} />
                            <IndicadorDoRelatorio rotulo="Renda disponível" valor={formatarMoeda(relatorio.rendaDisponivel)} destaque />
                        </div>
                    </Cartao>

                    <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
                        <Cartao>
                            <TituloDaSecao titulo="Aportes do período" descricao={`${relatorio.quantidadeAportes} registro(s)`} />

                            {relatorio.aportes.length === 0 ? (
                                <EstadoVazio
                                    Icone={BarChart3}
                                    titulo="Sem aportes no período"
                                    descricao="Ajuste as datas para incluir os aportes já realizados."
                                />
                            ) : (
                                <ul className="divide-y divide-portfy-borda">
                                    {relatorio.aportes.map((aporte) => (
                                        <li key={aporte.id} className="flex items-center justify-between py-2.5 text-sm">
                                            <span className="text-portfy-suave">{formatarData(aporte.dataAporte)}</span>
                                            <strong className="text-portfy-positivo">{formatarMoeda(aporte.valorAportado)}</strong>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </Cartao>

                        <Cartao>
                            <TituloDaSecao titulo="Posições" descricao={`${relatorio.posicoes.length} ativo(s) na carteira`} />

                            {relatorio.posicoes.length === 0 ? (
                                <EstadoVazio
                                    Icone={BarChart3}
                                    titulo="Carteira sem posições"
                                    descricao="Compre um ativo no módulo de Investimentos para vê-lo aqui."
                                />
                            ) : (
                                <ul className="divide-y divide-portfy-borda">
                                    {relatorio.posicoes.map((posicao) => (
                                        <li key={posicao.ticker} className="flex items-center justify-between py-2.5 text-sm">
                                            <span className="min-w-0">
                                                <strong className="block text-portfy-texto">{posicao.ticker}</strong>
                                                <small className="block text-xs text-portfy-suave">
                                                    {posicao.quantidade} cota(s)
                                                </small>
                                            </span>
                                            <span className="text-right">
                                                <span className="block font-semibold text-portfy-texto">
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
                        <TituloDaSecao titulo="Orçamentos" descricao="Teto, gasto e saldo por categoria" />

                        {relatorio.orcamentos.length === 0 ? (
                            <EstadoVazio
                                Icone={BarChart3}
                                titulo="Nenhum orçamento cadastrado"
                                descricao="Cadastre categorias para acompanhar o comprometimento da renda."
                            />
                        ) : (
                            <div className="overflow-x-auto">
                                <table className="w-full min-w-2xl text-left text-sm">
                                    <thead>
                                        <tr className="border-b border-portfy-borda text-xs uppercase tracking-wide text-portfy-suave">
                                            <th className="py-2 pr-4 font-semibold">Categoria</th>
                                            <th className="py-2 pr-4 text-right font-semibold">Limite</th>
                                            <th className="py-2 pr-4 text-right font-semibold">Gasto</th>
                                            <th className="py-2 pr-4 text-right font-semibold">Saldo</th>
                                            <th className="py-2 text-right font-semibold">% do salário</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-portfy-borda">
                                        {relatorio.orcamentos.map((orcamento) => (
                                            <tr key={orcamento.id} className="text-portfy-texto">
                                                <td className="py-3 pr-4">{orcamento.nomeCategoria}</td>
                                                <td className="py-3 pr-4 text-right">{formatarMoeda(orcamento.limiteDefinido)}</td>
                                                <td className="py-3 pr-4 text-right">{formatarMoeda(orcamento.valorGastoAtual)}</td>
                                                <td
                                                    className={
                                                        orcamento.estourado
                                                            ? 'py-3 pr-4 text-right text-portfy-negativo'
                                                            : 'py-3 pr-4 text-right text-portfy-positivo'
                                                    }
                                                >
                                                    {formatarMoeda(orcamento.saldoRestante)}
                                                </td>
                                                <td className="py-3 text-right">{formatarPercentual(orcamento.percentualDoSalario)}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </Cartao>
                </>
            )}
        </div>
    );
}
