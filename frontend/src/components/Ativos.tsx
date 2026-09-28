// Ativos.tsx é o componente de domínio visual e integração do módulo de ativos.
// Ele é responsável por conectar os dados vindo da Web API .NET com a interface reativa renderizada no navegador.

import { useEffect, useState } from 'react';
import { api } from '../services/api';

interface Ativo {
  ticker: string;
  nome: string;
  tipo: string;
  precoAtual: number;
  rentabilidadeSimulada: number;
}

export function Ativos() {
  const [ativos, setAtivos] = useState<Ativo[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<Ativo[]>('/ativo')
      .then(response => {
        setAtivos(response.data);
        setLoading(false);
      })
      .catch(error => {
        console.error('Erro ao conectar com a API:', error);
        setLoading(false);
      });
  }, []);

  if (loading) return <p>Carregando dados da Web API...</p>;

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