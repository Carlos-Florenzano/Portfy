// Ativos.tsx é o componente de domínio visual e integração do módulo de ativos.
// Ele consome o ativoService; nenhuma chamada HTTP é feita aqui diretamente.

import { useEffect, useState } from 'react';
import { mensagemDeErro } from '../services/api';
import { ativoService } from '../services/AtivoService';
import type { Ativo } from '../services/AtivoService';

export function Ativos() {
  const [ativos, setAtivos] = useState<Ativo[]>([]);
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState('');

  useEffect(() => {
    async function carregar() {
      try {
        setErro('');
        setAtivos(await ativoService.obterTodos());
      } catch (falha: unknown) {
        setErro(mensagemDeErro(falha));
      } finally {
        setLoading(false);
      }
    }

    void carregar();
  }, []);

  if (loading) return <p>Carregando dados da Web API...</p>;

  if (erro) return <p style={{ padding: '20px', color: '#e63946' }}>{erro}</p>;

  if (ativos.length === 0) return <p style={{ padding: '20px' }}>Nenhum ativo disponível.</p>;

  return (
    <div style={{ padding: '20px', fontFamily: 'system-ui, sans-serif' }}>
      <h2>Catálogo de Ativos (Portfy)</h2>
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {ativos.map(ativo => (
          <li key={ativo.ticker} style={{ margin: '12px 0', padding: '12px', border: '1px solid #444', borderRadius: '6px' }}>
            <strong>{ativo.ticker}</strong> - {ativo.nome} <small>({ativo.tipo})</small>
            <div style={{ marginTop: '4px', color: '#4caf50', fontWeight: 'bold' }}>
              R$ {ativo.precoAtual.toFixed(2)}
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
