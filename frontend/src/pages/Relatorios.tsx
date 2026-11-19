// Módulo "Relatórios": consolidação por período e exportação em CSV.
// O backend entrega os totais; a tela só formata e baixa o arquivo.
import { useCallback, useEffect, useRef, useState } from 'react';
import { BarChart3, CalendarDays, Download, RefreshCw } from 'lucide-react';
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

// Mantém só dígitos e monta dd/mm/aaaa enquanto a pessoa digita ou cola.
function mascararData(texto: string): string {
    const digitos = texto.replace(/\D/g, '').slice(0, 8);

    return digitos
        .replace(/^(\d{2})(\d)/, '$1/$2')
        .replace(/^(\d{2})\/(\d{2})(\d)/, '$1/$2/$3');
}

// dd/mm/aaaa → yyyy-mm-dd (contrato da API). Devolve null quando incompleta ou inválida.
function dataParaIso(texto: string): string | null {
    const partes = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(texto.trim());

    if (!partes) return null;

    const dia = Number(partes[1]);
    const mes = Number(partes[2]);
    const ano = Number(partes[3]);
    const data = new Date(ano, mes - 1, dia);

    if (
        data.getFullYear() !== ano ||
        data.getMonth() !== mes - 1 ||
        data.getDate() !== dia
    ) {
        return null;
    }

    return `${partes[3]}-${partes[2]}-${partes[1]}`;
}

function isoParaTexto(iso: string): string {
    const partes = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);

    return partes ? `${partes[3]}/${partes[2]}/${partes[1]}` : '';
}

// Campo de data do período: aceita digitação em dd/mm/aaaa e mantém o
// calendário nativo acessível pelo ícone, sem mudar o padrão visual da tela.
function CampoDeData({
    id,
    rotulo,
    valor,
    aoAlterar,
    descricaoDoCalendario,
}: {
    id: string;
    rotulo: string;
    valor: string;
    aoAlterar: (texto: string) => void;
    descricaoDoCalendario: string;
}) {
    const seletorNativo = useRef<HTMLInputElement>(null);
    const iso = dataParaIso(valor);

    function abrirCalendario() {
        const campo = seletorNativo.current;

        if (!campo) return;

        try {
            campo.showPicker();
        } catch {
            // Navegador sem seletor nativo: o campo de texto continua aceitando digitação.
            campo.focus();
        }
    }

    return (
        <div className="min-w-40 flex-1">
            <label className={classesDeRotulo} htmlFor={id}>
                {rotulo}
            </label>
            <div className="relative">
                <input
                    id={id}
                    className={`${classesDeEntrada} pr-11`}
                    type="text"
                    inputMode="numeric"
                    placeholder="dd/mm/aaaa"
                    maxLength={10}
                    value={valor}
                    onChange={(evento) => aoAlterar(mascararData(evento.target.value))}
                />
                <button
                    type="button"
                    onClick={abrirCalendario}
                    aria-label={descricaoDoCalendario}
                    className="absolute inset-y-0 right-0 flex w-10 items-center justify-center text-portfy-suave transition hover:text-portfy-acento focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-portfy-acento/30"
                >
                    <CalendarDays size={16} aria-hidden />
                </button>
                <input
                    ref={seletorNativo}
                    type="date"
                    tabIndex={-1}
                    aria-hidden="true"
                    className="pointer-events-none absolute inset-y-0 right-0 w-10 border-0 p-0 opacity-0"
                    value={iso ?? ''}
                    onChange={(evento) => aoAlterar(isoParaTexto(evento.target.value))}
                />
            </div>
        </div>
    );
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

    // Converte o texto digitado (dd/mm/aaaa) para o ISO aceito pela API.
    function periodoParaConsulta(): { inicio: string | undefined; fim: string | undefined } | null {
        const inicioDigitado = inicio.trim();
        const fimDigitado = fim.trim();
        const inicioIso = inicioDigitado === '' ? null : dataParaIso(inicioDigitado);
        const fimIso = fimDigitado === '' ? null : dataParaIso(fimDigitado);

        if (inicioDigitado !== '' && inicioIso === null) {
            setRecado({ tipo: 'erro', texto: 'Data inicial inválida. Use o formato dd/mm/aaaa.' });
            return null;
        }

        if (fimDigitado !== '' && fimIso === null) {
            setRecado({ tipo: 'erro', texto: 'Data final inválida. Use o formato dd/mm/aaaa.' });
            return null;
        }

        if (inicioIso && fimIso && inicioIso > fimIso) {
            setRecado({ tipo: 'erro', texto: 'A data inicial não pode ser posterior à data final.' });
            return null;
        }

        return { inicio: inicioIso ?? undefined, fim: fimIso ?? undefined };
    }

    async function gerar() {
        const periodo = periodoParaConsulta();

        if (!periodo) return;

        try {
            setCarregando(true);
            await carregar(periodo.inicio, periodo.fim);
            setRecado(null);
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setCarregando(false);
        }
    }

    async function exportar() {
        const periodo = periodoParaConsulta();

        if (!periodo) return;

        try {
            setBaixando(true);
            const arquivo = await relatorioService.exportarCsv(periodo.inicio, periodo.fim);
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
                    <CampoDeData
                        id="relatorio-inicio"
                        rotulo="De"
                        valor={inicio}
                        aoAlterar={setInicio}
                        descricaoDoCalendario="Escolher a data inicial no calendário"
                    />
                    <CampoDeData
                        id="relatorio-fim"
                        rotulo="Até"
                        valor={fim}
                        aoAlterar={setFim}
                        descricaoDoCalendario="Escolher a data final no calendário"
                    />

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
