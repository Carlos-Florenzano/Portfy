// Módulo "Aportes": registro de novos depósitos e histórico por período.
// O total do recorte vem calculado do backend (GET /aporte).
import { useCallback, useEffect, useState } from 'react';
import { ArrowDownToLine, CalendarRange, Plus } from 'lucide-react';
import { mensagemDeErro } from '../services/api';
import { aporteService } from '../services/AporteService';
import type { HistoricoDeAportes } from '../services/AporteService';
import { Aviso, Botao, Cartao, EstadoVazio, TituloDaSecao } from '../components/ui';
import { classesDeEntrada, classesDeRotulo, formatarData, formatarMoeda } from '../components/formatadores';

interface Recado {
    tipo: 'sucesso' | 'erro';
    texto: string;
}

export function Aportes() {
    const [historico, setHistorico] = useState<HistoricoDeAportes | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [recado, setRecado] = useState<Recado | null>(null);

    const [valor, setValor] = useState('');
    const [registrando, setRegistrando] = useState(false);

    const [inicio, setInicio] = useState('');
    const [fim, setFim] = useState('');

    const carregar = useCallback(async (inicioDoRecorte?: string, fimDoRecorte?: string) => {
        const dados = await aporteService.historico(inicioDoRecorte, fimDoRecorte);
        setHistorico(dados);
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

    async function registrar() {
        try {
            setRegistrando(true);
            const criado = await aporteService.criar(Number(valor) || 0);
            await carregar(inicio, fim);
            setValor('');
            setRecado({
                tipo: 'sucesso',
                texto: `${criado.mensagem} Saldo disponível: ${formatarMoeda(criado.saldoDisponivel)}.`,
            });
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        } finally {
            setRegistrando(false);
        }
    }

    async function filtrar() {
        try {
            await carregar(inicio, fim);
            setRecado(null);
        } catch (falha: unknown) {
            setRecado({ tipo: 'erro', texto: mensagemDeErro(falha) });
        }
    }

    const aportes = historico?.aportes ?? [];

    return (
        <div className="flex flex-col gap-7">
            {recado && <Aviso tipo={recado.tipo} texto={recado.texto} />}

            <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
                <Cartao>
                    <TituloDaSecao titulo="Novo aporte" descricao="O valor entra no saldo disponível da carteira." />

                    <label className={classesDeRotulo} htmlFor="valor-aporte">
                        Valor (R$)
                    </label>
                    <div className="flex flex-wrap gap-2">
                        <input
                            id="valor-aporte"
                            className={`${classesDeEntrada} max-w-56`}
                            type="number"
                            min="0"
                            step="0.01"
                            placeholder="0,00"
                            value={valor}
                            onChange={(evento) => setValor(evento.target.value)}
                        />
                        <Botao onClick={() => void registrar()} disabled={registrando || Number(valor) <= 0}>
                            <Plus size={15} aria-hidden />
                            {registrando ? 'Registrando...' : 'Registrar aporte'}
                        </Botao>
                    </div>
                </Cartao>

                <Cartao>
                    <TituloDaSecao titulo="Filtrar histórico" descricao="Recorte por data do aporte." />

                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                        <div>
                            <label className={classesDeRotulo} htmlFor="aporte-inicio">
                                De
                            </label>
                            <input
                                id="aporte-inicio"
                                className={classesDeEntrada}
                                type="date"
                                value={inicio}
                                onChange={(evento) => setInicio(evento.target.value)}
                            />
                        </div>
                        <div>
                            <label className={classesDeRotulo} htmlFor="aporte-fim">
                                Até
                            </label>
                            <input
                                id="aporte-fim"
                                className={classesDeEntrada}
                                type="date"
                                value={fim}
                                onChange={(evento) => setFim(evento.target.value)}
                            />
                        </div>
                    </div>

                    <div className="mt-4">
                        <Botao variante="secundario" onClick={() => void filtrar()}>
                            <CalendarRange size={15} aria-hidden />
                            Aplicar recorte
                        </Botao>
                    </div>
                </Cartao>
            </div>

            <Cartao>
                <TituloDaSecao
                    titulo="Histórico de aportes"
                    descricao={
                        historico
                            ? `${historico.quantidade} aporte(s) · total de ${formatarMoeda(historico.total)}`
                            : 'Aportes registrados'
                    }
                />

                {carregando ? (
                    <p className="text-sm text-portfy-suave">Carregando histórico...</p>
                ) : aportes.length === 0 ? (
                    <EstadoVazio
                        Icone={ArrowDownToLine}
                        titulo="Nenhum aporte no período"
                        descricao="Registre o primeiro aporte ou amplie o recorte de datas."
                    />
                ) : (
                    <div className="overflow-x-auto">
                        <table className="w-full min-w-xl text-left text-sm">
                            <thead>
                                <tr className="border-b border-portfy-borda text-xs uppercase tracking-wide text-portfy-suave">
                                    <th className="py-2 pr-4 font-semibold">Data</th>
                                    <th className="py-2 pr-4 font-semibold">Carteira</th>
                                    <th className="py-2 text-right font-semibold">Valor</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-portfy-borda">
                                {aportes.map((aporte) => (
                                    <tr key={aporte.id} className="text-portfy-texto">
                                        <td className="py-3 pr-4">{formatarData(aporte.dataAporte)}</td>
                                        <td className="py-3 pr-4 text-portfy-suave">#{aporte.carteiraId}</td>
                                        <td className="py-3 text-right font-semibold text-portfy-positivo">
                                            {formatarMoeda(aporte.valorAportado)}
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
