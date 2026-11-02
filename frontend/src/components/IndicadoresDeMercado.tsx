// IndicadoresDeMercado.tsx — contexto macroeconômico oficial do Dashboard.
// Consome mercadoService; nenhuma chamada HTTP acontece neste arquivo.
import { useEffect, useState } from 'react';
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
    <section className="panel">
      <div className="panel-header">
        <div>
          <h2>Mercado hoje</h2>
          <p>Indicadores oficiais do Banco Central (SGS)</p>
        </div>
        <button className="panel-button" onClick={atualizar} disabled={carregando}>
          Atualizar
        </button>
      </div>

      {carregando && <p className="card-description">Carregando indicadores...</p>}

      {!carregando && erro !== '' && (
        <p className="card-description" role="alert">{erro}</p>
      )}

      {!carregando && erro === '' && resposta && resposta.indicadores.length === 0 && (
        <p className="card-description">Nenhum indicador publicado no momento.</p>
      )}

      {!carregando && erro === '' && resposta && resposta.indicadores.length > 0 && (
        <>
          <div className="cards">
            {resposta.indicadores.map((indicador) => (
              <div className="card" key={indicador.codigo}>
                <div className="card-header">
                  <span>{indicador.codigo}</span>
                </div>
                <h2>{formatarValor(indicador)}</h2>
                <p className="card-description">
                  {indicador.descricao} · {formatarData(indicador.data)}
                </p>
              </div>
            ))}
          </div>

          {resposta.avisos.length > 0 && (
            <p className="card-description" role="status">{resposta.avisos.join(' ')}</p>
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
