// IndicadoresDeMercado.tsx — contexto macroeconômico oficial do Dashboard.
// Consome mercadoService; nenhuma chamada HTTP acontece neste arquivo.
import { useEffect, useState } from 'react';
import { RefreshCw } from 'lucide-react';
import { mensagemDeErro } from '../services/api';
import { mercadoService } from '../services/mercadoService';
import type { IndicadorDeMercado, RespostaDeIndicadores } from '../types/mercado';

export function IndicadoresDeMercado() {
  const [resposta, setResposta] = useState<RespostaDeIndicadores | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  // Cada incremento refaz a busca; o botão "Atualizar" só troca o gatilho.
  const [tentativa, setTentativa] = useState(0);

  useEffect(() => {
    let montado = true;

    async function carregar() {
      try {
        const dados = await mercadoService.indicadores();

        if (!montado) return;

        setResposta(dados);
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
  }, [tentativa]);

  function atualizar() {
    setCarregando(true);
    setErro('');
    setTentativa((valor) => valor + 1);
  }

  return (
    <section className="rounded-2xl border border-portfy-borda bg-portfy-superficie p-6">
      <div className="mb-5 flex items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-bold text-portfy-texto">Mercado hoje</h2>
          <p className="mt-1 text-sm text-portfy-suave">Indicadores oficiais do Banco Central (SGS)</p>
        </div>
        <button
          type="button"
          className="inline-flex items-center gap-2 rounded-xl border border-portfy-borda px-3.5 py-2 text-xs font-semibold text-portfy-texto transition hover:bg-portfy-superficie-alta disabled:opacity-50"
          onClick={atualizar}
          disabled={carregando}
        >
          <RefreshCw size={14} aria-hidden />
          Atualizar
        </button>
      </div>

      {carregando && <p className="text-sm text-portfy-suave">Carregando indicadores...</p>}

      {!carregando && erro !== '' && (
        <p className="text-sm text-portfy-negativo" role="alert">{erro}</p>
      )}

      {!carregando && erro === '' && resposta && resposta.indicadores.length === 0 && (
        <p className="text-sm text-portfy-suave">Nenhum indicador publicado no momento.</p>
      )}

      {!carregando && erro === '' && resposta && resposta.indicadores.length > 0 && (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {resposta.indicadores.map((indicador) => (
              <div
                key={indicador.codigo}
                className="rounded-xl border border-portfy-borda bg-portfy-fundo px-4 py-3"
              >
                <span className="block text-xs font-semibold uppercase tracking-wide text-portfy-acento-claro">
                  {indicador.codigo}
                </span>
                <strong className="mt-1 block text-lg font-bold text-portfy-texto">
                  {formatarValor(indicador)}
                </strong>
                <p className="mt-1 text-xs text-portfy-suave">
                  {indicador.descricao} · {formatarData(indicador.data)}
                </p>
              </div>
            ))}
          </div>

          {resposta.avisos.length > 0 && (
            <p className="mt-3 text-xs text-portfy-alerta" role="status">{resposta.avisos.join(' ')}</p>
          )}
        </>
      )}
    </section>
  );
}

// Exibição apenas: o valor autoritativo é o que veio do backend (arch §11.2).
function formatarValor(indicador: IndicadorDeMercado): string {
  if (indicador.unidade === 'BRL') {
    return indicador.valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  return `${indicador.valor.toLocaleString('pt-BR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  })}%`;
}

function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-');

  return ano && mes && dia ? `${dia}/${mes}/${ano}` : data;
}
