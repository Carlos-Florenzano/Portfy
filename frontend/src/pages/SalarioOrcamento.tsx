// Módulo "Salário e Orçamento": renda mensal do usuário e tetos por categoria.
// Cada item de orçamento cuida da própria edição, despesa e exclusão, e pede
// recarga da lista ao pai — a lista continua sendo a fonte única de verdade.
import { useCallback, useEffect, useState } from 'react';
import { Check, Pencil, Plus, Receipt, Trash2, Wallet, X } from 'lucide-react';
import { mensagemDeErro } from '../services/api';
import { orcamentoService } from '../services/OrcamentoService';
import { usuarioService } from '../services/UsuarioService';
import type { Usuario } from '../services/UsuarioService';
import type { ListaOrcamentos, Orcamento } from '../types/orcamento';
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

function ItemDeOrcamento({
    orcamento,
    onRecarregar,
    onRecado,
}: {
    orcamento: Orcamento;
    onRecarregar: () => Promise<void>;
    onRecado: (recado: Recado) => void;
}) {
    const [editando, setEditando] = useState(false);
    const [lancandoDespesa, setLancandoDespesa] = useState(false);
    const [limite, setLimite] = useState(orcamento.limiteDefinido.toString());
    const [percentual, setPercentual] = useState(orcamento.percentualDoSalario.toString());
    const [ehFixo, setEhFixo] = useState(orcamento.ehFixo);
    const [valorDespesa, setValorDespesa] = useState('');
    const [ocupado, setOcupado] = useState(false);

    // Barra de progresso é apresentação: proporção entre gasto e teto, sem tocar
    // em valores financeiros (esses chegam prontos do backend).
    const proporcao = orcamento.limiteDefinido > 0
        ? Math.min(100, (orcamento.valorGastoAtual / orcamento.limiteDefinido) * 100)
        : 0;

    async function salvarEdicao() {
        try {
            setOcupado(true);
            await orcamentoService.atualizar(orcamento.id, {
                limiteDefinido: Number(limite) || 0,
                percentualDoSalario: Number(percentual) || 0,
                ehFixo,
            });
            await onRecarregar();
            setEditando(false);
            onRecado({ tipo: 'sucesso', texto: `Orçamento de ${orcamento.nomeCategoria} atualizado.` });
        } catch (falha: unknown) {
            onRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setOcupado(false);
        }
    }

    async function lancarDespesa() {
        try {
            setOcupado(true);
            await orcamentoService.adicionarDespesaNaCategoria(orcamento.id, Number(valorDespesa) || 0);
            await onRecarregar();
            setValorDespesa('');
            setLancandoDespesa(false);
            onRecado({ tipo: 'sucesso', texto: 'Despesa lançada com sucesso!' });
        } catch (falha: unknown) {
            onRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setOcupado(false);
        }
    }

    async function excluir() {
        const confirmou = window.confirm(
            `Excluir o orçamento de "${orcamento.nomeCategoria}"? Esta ação não pode ser desfeita.`,
        );

        if (!confirmou) return;

        try {
            setOcupado(true);
            await orcamentoService.excluir(orcamento.id);
            await onRecarregar();
            onRecado({ tipo: 'sucesso', texto: `Orçamento de ${orcamento.nomeCategoria} removido.` });
        } catch (falha: unknown) {
            onRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setOcupado(false);
        }
    }

    return (
        <li className="py-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="min-w-0">
                    <div className="flex items-center gap-2">
                        <strong className="text-sm text-portfy-texto">{orcamento.nomeCategoria}</strong>
                        {orcamento.ehFixo && (
                            <span className="rounded-full bg-portfy-acento/15 px-2 py-0.5 text-[11px] font-semibold text-portfy-acento-claro">
                                fixo
                            </span>
                        )}
                        {orcamento.estourado && (
                            <span className="rounded-full bg-portfy-negativo/15 px-2 py-0.5 text-[11px] font-semibold text-portfy-negativo">
                                estourado
                            </span>
                        )}
                    </div>
                    <small className="mt-0.5 block text-xs text-portfy-suave">
                        {formatarMoeda(orcamento.valorGastoAtual)} de {formatarMoeda(orcamento.limiteDefinido)} ·{' '}
                        {formatarPercentual(orcamento.percentualDoSalario)} do salário
                    </small>
                </div>

                <span className="flex items-center gap-1">
                    <Botao variante="secundario" onClick={() => setLancandoDespesa((valor) => !valor)} disabled={ocupado}>
                        <Receipt size={15} aria-hidden />
                        Despesa
                    </Botao>
                    <Botao variante="secundario" onClick={() => setEditando((valor) => !valor)} disabled={ocupado}>
                        <Pencil size={15} aria-hidden />
                        Editar
                    </Botao>
                    <Botao variante="perigo" onClick={() => void excluir()} disabled={ocupado}>
                        <Trash2 size={15} aria-hidden />
                        Excluir
                    </Botao>
                </span>
            </div>

            <div className="mt-3 h-1.5 w-full overflow-hidden rounded-full bg-portfy-superficie-alta">
                <div
                    className={`h-full rounded-full ${orcamento.estourado ? 'bg-portfy-negativo' : 'bg-portfy-positivo'}`}
                    style={{ width: `${proporcao}%` }}
                />
            </div>

            <div className="mt-2 flex items-center justify-between text-xs text-portfy-suave">
                <span>{formatarPercentual(proporcao)} do teto utilizado</span>
                <span>Saldo restante: {formatarMoeda(orcamento.saldoRestante)}</span>
            </div>

            {lancandoDespesa && (
                <div className="mt-3 flex flex-wrap items-end gap-2 rounded-xl border border-portfy-borda bg-portfy-fundo p-3">
                    <div className="min-w-40 flex-1">
                        <label className={classesDeRotulo} htmlFor={`despesa-${orcamento.id}`}>
                            Valor da despesa (R$)
                        </label>
                        <input
                            id={`despesa-${orcamento.id}`}
                            className={classesDeEntrada}
                            type="number"
                            min="0"
                            step="0.01"
                            value={valorDespesa}
                            onChange={(evento) => setValorDespesa(evento.target.value)}
                        />
                    </div>
                    <Botao onClick={() => void lancarDespesa()} disabled={ocupado || Number(valorDespesa) <= 0}>
                        <Check size={15} aria-hidden />
                        Lançar
                    </Botao>
                </div>
            )}

            {editando && (
                <div className="mt-3 flex flex-wrap items-end gap-2 rounded-xl border border-portfy-borda bg-portfy-fundo p-3">
                    <div className="min-w-36 flex-1">
                        <label className={classesDeRotulo} htmlFor={`limite-${orcamento.id}`}>
                            Limite (R$)
                        </label>
                        <input
                            id={`limite-${orcamento.id}`}
                            className={classesDeEntrada}
                            type="number"
                            min="0"
                            step="0.01"
                            value={limite}
                            onChange={(evento) => setLimite(evento.target.value)}
                        />
                    </div>
                    <div className="min-w-28 flex-1">
                        <label className={classesDeRotulo} htmlFor={`percentual-${orcamento.id}`}>
                            % do salário
                        </label>
                        <input
                            id={`percentual-${orcamento.id}`}
                            className={classesDeEntrada}
                            type="number"
                            min="0"
                            max="100"
                            step="0.5"
                            value={percentual}
                            onChange={(evento) => setPercentual(evento.target.value)}
                        />
                    </div>
                    <label className="flex items-center gap-2 pb-2.5 text-sm text-portfy-suave">
                        <input
                            type="checkbox"
                            checked={ehFixo}
                            onChange={(evento) => setEhFixo(evento.target.checked)}
                            className="size-4 accent-portfy-acento"
                        />
                        Gasto fixo
                    </label>
                    <Botao onClick={() => void salvarEdicao()} disabled={ocupado}>
                        <Check size={15} aria-hidden />
                        Salvar
                    </Botao>
                    <Botao variante="secundario" onClick={() => setEditando(false)} disabled={ocupado}>
                        <X size={15} aria-hidden />
                        Cancelar
                    </Botao>
                </div>
            )}
        </li>
    );
}

export function SalarioOrcamento({
    usuario,
    onSalarioAtualizado,
}: {
    usuario: Usuario;
    onSalarioAtualizado: (salarioMensal: number) => void;
}) {
    const [lista, setLista] = useState<ListaOrcamentos | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [recado, setRecado] = useState<Recado | null>(null);

    const [salvandoSalario, setSalvandoSalario] = useState(false);

    // Campo de salário: `null` significa "espelhar a renda do usuário". Só existe
    // edição local enquanto o usuário digita; depois de salvar volta a espelhar.
    // Isso dispensa efeito de sincronização entre prop e estado.
    const [edicaoDoSalario, setEdicaoDoSalario] = useState<string | null>(null);
    const salario = edicaoDoSalario ?? usuario.salarioMensal.toString();

    const [categoria, setCategoria] = useState('');
    const [limite, setLimite] = useState('');
    const [percentual, setPercentual] = useState('');
    const [ehFixo, setEhFixo] = useState(false);
    const [criando, setCriando] = useState(false);

    const recarregar = useCallback(async () => {
        const dados = await orcamentoService.obterTodos();
        setLista(dados);
    }, []);

    useEffect(() => {
        let montado = true;

        async function carregar() {
            try {
                const dados = await orcamentoService.obterTodos();

                if (!montado) return;

                setLista(dados);
            } catch (falha: unknown) {
                if (montado) setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
            } finally {
                if (montado) setCarregando(false);
            }
        }

        void carregar();

        return () => {
            montado = false;
        };
    }, []);

    async function atualizarSalario() {
        try {
            setSalvandoSalario(true);
            const resposta = await usuarioService.atualizarSalario(usuario.id, Number(salario) || 0);
            onSalarioAtualizado(resposta.novoSalario);
            setEdicaoDoSalario(null);
            setRecado({ tipo: 'sucesso', texto: resposta.mensagem });
            await recarregar();
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setSalvandoSalario(false);
        }
    }

    async function criarOrcamento() {
        try {
            setCriando(true);
            await orcamentoService.criar({
                nomeCategoria: categoria.trim(),
                limiteDefinido: Number(limite) || 0,
                percentualDoSalario: Number(percentual) || 0,
                ehFixo,
            });
            await recarregar();
            setCategoria('');
            setLimite('');
            setPercentual('');
            setEhFixo(false);
            setRecado({ tipo: 'sucesso', texto: 'Orçamento criado com sucesso!' });
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setCriando(false);
        }
    }

    const orcamentos = lista?.orcamentos ?? [];

    return (
        <div className="flex flex-col gap-7">
            {recado && <Aviso tipo={recado.tipo} texto={recado.texto} />}

            <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
                <Cartao>
                    <TituloDaSecao
                        titulo="Renda mensal"
                        descricao="Valor recebido por mês. Os percentuais dos orçamentos se apoiam nele."
                    />

                    <p className="mb-4 flex items-center gap-2 text-2xl font-bold text-portfy-texto">
                        <span className="flex size-9 items-center justify-center rounded-xl bg-portfy-acento/15 text-portfy-acento-claro">
                            <Wallet size={18} aria-hidden />
                        </span>
                        {formatarMoeda(usuario.salarioMensal)}
                    </p>

                    <label className={classesDeRotulo} htmlFor="novo-salario">
                        Novo salário (R$)
                    </label>
                    <div className="flex flex-wrap gap-2">
                        <input
                            id="novo-salario"
                            className={`${classesDeEntrada} max-w-56`}
                            type="number"
                            min="0"
                            step="0.01"
                            value={salario}
                            onChange={(evento) => setEdicaoDoSalario(evento.target.value)}
                        />
                        <Botao
                            onClick={() => void atualizarSalario()}
                            disabled={salvandoSalario || Number(salario) <= 0}
                        >
                            <Check size={15} aria-hidden />
                            {salvandoSalario ? 'Salvando...' : 'Atualizar salário'}
                        </Botao>
                    </div>
                </Cartao>

                <Cartao>
                    <TituloDaSecao
                        titulo="Novo orçamento"
                        descricao="Defina o teto de uma categoria de gasto."
                    />

                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                        <div className="sm:col-span-2">
                            <label className={classesDeRotulo} htmlFor="nova-categoria">
                                Categoria
                            </label>
                            <input
                                id="nova-categoria"
                                className={classesDeEntrada}
                                placeholder="Ex.: Alimentação"
                                maxLength={60}
                                value={categoria}
                                onChange={(evento) => setCategoria(evento.target.value)}
                            />
                        </div>

                        <div>
                            <label className={classesDeRotulo} htmlFor="novo-limite">
                                Limite (R$)
                            </label>
                            <input
                                id="novo-limite"
                                className={classesDeEntrada}
                                type="number"
                                min="0"
                                step="0.01"
                                value={limite}
                                onChange={(evento) => setLimite(evento.target.value)}
                            />
                        </div>

                        <div>
                            <label className={classesDeRotulo} htmlFor="novo-percentual">
                                % do salário
                            </label>
                            <input
                                id="novo-percentual"
                                className={classesDeEntrada}
                                type="number"
                                min="0"
                                max="100"
                                step="0.5"
                                value={percentual}
                                onChange={(evento) => setPercentual(evento.target.value)}
                            />
                        </div>
                    </div>

                    <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
                        <label className="flex items-center gap-2 text-sm text-portfy-suave">
                            <input
                                type="checkbox"
                                checked={ehFixo}
                                onChange={(evento) => setEhFixo(evento.target.checked)}
                                className="size-4 accent-portfy-acento"
                            />
                            Gasto fixo
                        </label>

                        <Botao
                            onClick={() => void criarOrcamento()}
                            disabled={criando || categoria.trim() === '' || Number(limite) <= 0}
                        >
                            <Plus size={15} aria-hidden />
                            {criando ? 'Criando...' : 'Criar orçamento'}
                        </Botao>
                    </div>
                </Cartao>
            </div>

            <Cartao>
                <TituloDaSecao
                    titulo="Categorias"
                    descricao={
                        lista
                            ? `${formatarMoeda(lista.totalGastoAtual)} gastos de ${formatarMoeda(lista.totalLimiteDefinido)} orçados · ${formatarPercentual(lista.percentualComprometido)} do salário comprometido`
                            : 'Seus tetos por categoria'
                    }
                />

                {carregando ? (
                    <p className="text-sm text-portfy-suave">Carregando orçamentos...</p>
                ) : orcamentos.length === 0 ? (
                    <EstadoVazio
                        Icone={Wallet}
                        titulo="Nenhuma categoria cadastrada"
                        descricao="Comece pela categoria que mais pesa no mês: moradia, alimentação ou transporte."
                    />
                ) : (
                    <ul className="divide-y divide-portfy-borda">
                        {orcamentos.map((orcamento) => (
                            <ItemDeOrcamento
                                key={orcamento.id}
                                orcamento={orcamento}
                                onRecarregar={recarregar}
                                onRecado={setRecado}
                            />
                        ))}
                    </ul>
                )}

                {lista && orcamentos.length > 0 && (
                    <div className="mt-4 flex flex-wrap justify-between gap-2 border-t border-portfy-borda pt-4 text-sm">
                        <span className="text-portfy-suave">Saldo restante total</span>
                        <strong className="text-portfy-texto">{formatarMoeda(lista.totalSaldoRestante)}</strong>
                    </div>
                )}
            </Cartao>
        </div>
    );
}
