// Módulo "Investimentos": catálogo de ativos, posições da carteira e compra.
// Os valores consolidados vêm do relatório do backend; a tela só exibe.
import { useCallback, useEffect, useState } from 'react';
import { BadgePlus, LineChart, Plus, ShoppingCart, TrendingUp } from 'lucide-react';
import { mensagemDeErro } from '../services/api';
import { carteiraService } from '../services/CarteiraService';
import { ativoService } from '../services/AtivoService';
import type { Ativo } from '../services/AtivoService';
import { relatorioService } from '../services/RelatorioService';
import type { Relatorio } from '../types/relatorio';
import { Aviso, Botao, Cartao, EstadoVazio, TituloDaSecao } from '../components/ui';
import {
    classesDeEntrada,
    classesDeRotulo,
    formatarMoeda,
    formatarPercentual,
} from '../components/formatadores';

interface Recado {
    tipo: 'sucesso' | 'erro';
    texto: string;
}

const TIPOS_DE_ATIVO = [
    { valor: 'Acao', rotulo: 'Ação' },
    { valor: 'Fii', rotulo: 'Fundo imobiliário' },
    { valor: 'RendaFixa', rotulo: 'Renda fixa' },
    { valor: 'Cripto', rotulo: 'Cripto' },
    { valor: 'Etf', rotulo: 'ETF' },
];

export function Investimentos() {
    const [relatorio, setRelatorio] = useState<Relatorio | null>(null);
    const [catalogo, setCatalogo] = useState<Ativo[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [recado, setRecado] = useState<Recado | null>(null);

    const [ticker, setTicker] = useState('');
    const [quantidade, setQuantidade] = useState('1');
    const [comprando, setComprando] = useState(false);

    const [cadastrando, setCadastrando] = useState(false);
    const [novoTicker, setNovoTicker] = useState('');
    const [novoNome, setNovoNome] = useState('');
    const [novoTipo, setNovoTipo] = useState('Acao');
    const [novoPreco, setNovoPreco] = useState('');
    const [salvandoAtivo, setSalvandoAtivo] = useState(false);

    const carregar = useCallback(async () => {
        const [dadosDoRelatorio, ativos] = await Promise.all([
            relatorioService.obter(),
            ativoService.obterTodos(),
        ]);

        setRelatorio(dadosDoRelatorio);
        setCatalogo(ativos);
        setTicker((atual) => (atual !== '' ? atual : (ativos[0]?.ticker ?? '')));
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

    async function comprar() {
        try {
            setComprando(true);
            const operacao = await carteiraService.comprar(ticker, Number(quantidade) || 0);
            await carregar();
            setQuantidade('1');
            setRecado({
                tipo: 'sucesso',
                texto: `${operacao.mensagem} ${operacao.quantidade}x ${operacao.ticker} por ${formatarMoeda(operacao.precoUnitario)}.`,
            });
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setComprando(false);
        }
    }

    async function cadastrarAtivo() {
        try {
            setSalvandoAtivo(true);
            const ativo = await ativoService.criar({
                ticker: novoTicker.trim().toUpperCase(),
                nome: novoNome.trim(),
                tipo: novoTipo,
                precoAtual: Number(novoPreco) || 0,
            });
            await carregar();
            setTicker(ativo.ticker);
            setNovoTicker('');
            setNovoNome('');
            setNovoPreco('');
            setCadastrando(false);
            setRecado({ tipo: 'sucesso', texto: `Ativo ${ativo.ticker} cadastrado no catálogo.` });
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setSalvandoAtivo(false);
        }
    }

    const posicoes = relatorio?.posicoes ?? [];

    return (
        <div className="flex flex-col gap-7">
            {recado && <Aviso tipo={recado.tipo} texto={recado.texto} />}

            <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
                <Cartao>
                    <span className="text-sm font-semibold text-portfy-suave">Patrimônio total</span>
                    <p className="mt-2 text-2xl font-bold text-portfy-texto">
                        {formatarMoeda(relatorio?.patrimonioTotal ?? 0)}
                    </p>
                </Cartao>
                <Cartao>
                    <span className="text-sm font-semibold text-portfy-suave">Saldo disponível</span>
                    <p className="mt-2 text-2xl font-bold text-portfy-positivo">
                        {formatarMoeda(relatorio?.saldoDisponivel ?? 0)}
                    </p>
                </Cartao>
                <Cartao>
                    <span className="text-sm font-semibold text-portfy-suave">Total investido</span>
                    <p className="mt-2 text-2xl font-bold text-portfy-texto">
                        {formatarMoeda(relatorio?.totalInvestido ?? 0)}
                    </p>
                </Cartao>
            </div>

            <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
                <Cartao>
                    <TituloDaSecao titulo="Comprar ativo" descricao="A compra usa o saldo disponível da carteira." />

                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                        <div>
                            <label className={classesDeRotulo} htmlFor="ticker-compra">
                                Ativo
                            </label>
                            <select
                                id="ticker-compra"
                                className={classesDeEntrada}
                                value={ticker}
                                onChange={(evento) => setTicker(evento.target.value)}
                            >
                                {catalogo.map((ativo) => (
                                    <option key={ativo.ticker} value={ativo.ticker}>
                                        {ativo.ticker} — {ativo.nome}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label className={classesDeRotulo} htmlFor="quantidade-compra">
                                Quantidade
                            </label>
                            <input
                                id="quantidade-compra"
                                className={classesDeEntrada}
                                type="number"
                                min="1"
                                step="1"
                                value={quantidade}
                                onChange={(evento) => setQuantidade(evento.target.value)}
                            />
                        </div>
                    </div>

                    <div className="mt-4">
                        <Botao
                            onClick={() => void comprar()}
                            disabled={comprando || ticker === '' || Number(quantidade) <= 0}
                        >
                            <ShoppingCart size={15} aria-hidden />
                            {comprando ? 'Comprando...' : 'Comprar'}
                        </Botao>
                    </div>
                </Cartao>

                <Cartao>
                    <TituloDaSecao
                        titulo="Catálogo"
                        descricao={`${catalogo.length} ativo(s) disponíveis para negociação`}
                        acao={
                            <Botao variante="secundario" onClick={() => setCadastrando((valor) => !valor)}>
                                <BadgePlus size={15} aria-hidden />
                                Novo ativo
                            </Botao>
                        }
                    />

                    {cadastrando && (
                        <div className="mb-4 grid grid-cols-1 gap-3 rounded-xl border border-portfy-borda bg-portfy-fundo p-4 sm:grid-cols-2">
                            <div>
                                <label className={classesDeRotulo} htmlFor="novo-ticker">
                                    Ticker
                                </label>
                                <input
                                    id="novo-ticker"
                                    className={classesDeEntrada}
                                    placeholder="Ex.: ITSA4"
                                    maxLength={10}
                                    value={novoTicker}
                                    onChange={(evento) => setNovoTicker(evento.target.value)}
                                />
                            </div>
                            <div>
                                <label className={classesDeRotulo} htmlFor="novo-nome">
                                    Nome
                                </label>
                                <input
                                    id="novo-nome"
                                    className={classesDeEntrada}
                                    placeholder="Ex.: Itaúsa PN"
                                    maxLength={120}
                                    value={novoNome}
                                    onChange={(evento) => setNovoNome(evento.target.value)}
                                />
                            </div>
                            <div>
                                <label className={classesDeRotulo} htmlFor="novo-tipo">
                                    Tipo
                                </label>
                                <select
                                    id="novo-tipo"
                                    className={classesDeEntrada}
                                    value={novoTipo}
                                    onChange={(evento) => setNovoTipo(evento.target.value)}
                                >
                                    {TIPOS_DE_ATIVO.map((tipo) => (
                                        <option key={tipo.valor} value={tipo.valor}>
                                            {tipo.rotulo}
                                        </option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label className={classesDeRotulo} htmlFor="novo-preco">
                                    Preço atual (R$)
                                </label>
                                <input
                                    id="novo-preco"
                                    className={classesDeEntrada}
                                    type="number"
                                    min="0"
                                    step="0.01"
                                    value={novoPreco}
                                    onChange={(evento) => setNovoPreco(evento.target.value)}
                                />
                            </div>
                            <div className="sm:col-span-2">
                                <Botao
                                    onClick={() => void cadastrarAtivo()}
                                    disabled={
                                        salvandoAtivo ||
                                        novoTicker.trim() === '' ||
                                        novoNome.trim() === '' ||
                                        Number(novoPreco) <= 0
                                    }
                                >
                                    <Plus size={15} aria-hidden />
                                    {salvandoAtivo ? 'Cadastrando...' : 'Cadastrar ativo'}
                                </Botao>
                            </div>
                        </div>
                    )}

                    <ul className="divide-y divide-portfy-borda">
                        {catalogo.map((ativo) => (
                            <li key={ativo.ticker} className="flex items-center justify-between gap-3 py-2.5">
                                <span className="min-w-0">
                                    <strong className="block text-sm text-portfy-texto">{ativo.ticker}</strong>
                                    <small className="block truncate text-xs text-portfy-suave">{ativo.nome}</small>
                                </span>
                                <span className="text-right">
                                    <span className="block text-sm font-semibold text-portfy-texto">
                                        {formatarMoeda(ativo.precoAtual)}
                                    </span>
                                    <small className="block text-xs text-portfy-suave">{ativo.tipo}</small>
                                </span>
                            </li>
                        ))}
                    </ul>
                </Cartao>
            </div>

            <Cartao>
                <TituloDaSecao titulo="Minha carteira" descricao="Posições com preço médio, valor atual e resultado" />

                {carregando ? (
                    <p className="text-sm text-portfy-suave">Carregando carteira...</p>
                ) : posicoes.length === 0 ? (
                    <EstadoVazio
                        Icone={LineChart}
                        titulo="Nenhuma posição na carteira"
                        descricao="Escolha um ativo do catálogo e registre a primeira compra."
                    />
                ) : (
                    <div className="overflow-x-auto">
                        <table className="w-full min-w-2xl text-left text-sm">
                            <thead>
                                <tr className="border-b border-portfy-borda text-xs uppercase tracking-wide text-portfy-suave">
                                    <th className="py-2 pr-4 font-semibold">Ativo</th>
                                    <th className="py-2 pr-4 text-right font-semibold">Qtd.</th>
                                    <th className="py-2 pr-4 text-right font-semibold">Preço médio</th>
                                    <th className="py-2 pr-4 text-right font-semibold">Preço atual</th>
                                    <th className="py-2 pr-4 text-right font-semibold">Valor atual</th>
                                    <th className="py-2 text-right font-semibold">Resultado</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-portfy-borda">
                                {posicoes.map((posicao) => (
                                    <tr key={posicao.ticker} className="text-portfy-texto">
                                        <td className="py-3 pr-4">
                                            <strong className="block">{posicao.ticker}</strong>
                                            <small className="block text-xs text-portfy-suave">{posicao.nome}</small>
                                        </td>
                                        <td className="py-3 pr-4 text-right">{posicao.quantidade}</td>
                                        <td className="py-3 pr-4 text-right">{formatarMoeda(posicao.precoMedio)}</td>
                                        <td className="py-3 pr-4 text-right">{formatarMoeda(posicao.precoAtual)}</td>
                                        <td className="py-3 pr-4 text-right font-semibold">
                                            {formatarMoeda(posicao.valorAtual)}
                                        </td>
                                        <td
                                            className={
                                                posicao.resultado >= 0
                                                    ? 'py-3 text-right text-portfy-positivo'
                                                    : 'py-3 text-right text-portfy-negativo'
                                            }
                                        >
                                            <span className="flex items-center justify-end gap-1">
                                                <TrendingUp size={14} aria-hidden />
                                                {formatarMoeda(posicao.resultado)} ({formatarPercentual(posicao.rentabilidadePercentual)})
                                            </span>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </Cartao>
        </div>
    );
}
