// Espelho dos DTOs de mercado do backend (arch §10.7). Os nomes de campo são os
// mesmos do JSON; `undefined` nunca aparece, o backend devolve null quando falta.

export interface IndicadorDeMercado {
  codigo: string;
  descricao: string;
  valor: number;
  // DateOnly serializa como "AAAA-MM-DD".
  data: string;
  unidade: string;
  serieSgs: string;
  fonte: string;
}

export interface RespostaDeIndicadores {
  indicadores: IndicadorDeMercado[];
  avisos: string[];
}
