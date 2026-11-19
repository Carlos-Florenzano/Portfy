# Integrações de Mercado — configuração e contrato

**Escopo:** como o Portfy obtém cotação, fundamentos e indicadores macroeconômicos de
fontes externas, como configurar as credenciais e o que cada status HTTP significa.
**Decisão de arquitetura:** ADR-020 em `docs/arch.md` §19.

---

## 1. Fontes e responsabilidades

| Fonte | Cobre | Contrato | Fallback |
|---|---|---|---|
| **BRAPI** (`brapi.dev`) | ações da B3, FIIs e cripto; múltiplos (P/L, LPA) | `IProvedorDeCotacoes`, `IProvedorDeFundamentos` (cobertura parcial) | cotação: FMP; fundamentos: é o fallback da FMP |
| **FMP** (`financialmodelingprep.com`) | ativos globais; perfil e fundamentos | `IProvedorDeCotacoes`, `IProvedorDeFundamentos` (cobertura completa) | cotação: BRAPI |
| **Banco Central / SGS** (`api.bcb.gov.br`) | IPCA (433), Selic mensal (432), CDI mensal (4389), dólar comercial (1) | `IProvedorDeIndicadores` | **nenhum** — é fonte oficial e única |

Prioridade de cotação: **ticker da B3** (`^[A-Z]{4}[0-9]{1,2}$`, ex.: `PETR4`, `HGLG11`) → BRAPI
primeiro; qualquer outro (`AAPL`, ETF global) → FMP primeiro. Fundamentos: FMP primeiro, BRAPI
como fallback declarado parcial.

---

## 2. Configuração

### 2.1 Credenciais (nunca no repositório)

```bash
# Linux/macOS
export Integracoes__TokenBrapi='seu-token'
export Integracoes__ChaveFmp='sua-chave'

# Windows (PowerShell)
$env:Integracoes__TokenBrapi = 'seu-token'
$env:Integracoes__ChaveFmp = 'sua-chave'
```

Alternativa em desenvolvimento: `dotnet user-secrets set "Integracoes:TokenBrapi" "<token>"`.

### 2.2 Seção opcional no arquivo de configuração

Os valores abaixo já são o padrão no código (`Services/Integracoes/OpcoesDeIntegracao.cs`);
a seção só é necessária para sobrescrever URL, timeout, TTL ou limite.

```json
"Integracoes": {
  "UrlBrapi": "https://brapi.dev",
  "TokenBrapi": "",
  "UrlFmp": "https://financialmodelingprep.com",
  "ChaveFmp": "",
  "UrlBcb": "https://api.bcb.gov.br",
  "SegundosDeTimeout": 8,
  "SegundosDeBloqueioDoProvedor": 60,
  "SegundosDeCacheDeCotacao": 60,
  "SegundosDeCacheDeFundamento": 43200,
  "SegundosDeCacheDeIndicador": 3600
}
```

| Chave | Default | Para que serve |
|---|---|---|
| `SegundosDeTimeout` | 8 | tempo máximo de cada chamada externa |
| `SegundosDeBloqueioDoProvedor` | 60 | quanto tempo uma fonte que falhou fica fora da rotação |
| `SegundosDeCacheDeCotacao` | 60 | protege a cota diária do plano Basic da FMP |
| `SegundosDeCacheDeFundamento` | 43200 | fundamentos mudam por balanço (12 h) |
| `SegundosDeCacheDeIndicador` | 3600 | séries do SGS são publicadas uma vez por dia/mês |

### 2.3 Fonte não configurada

Sem `TokenBrapi`/`ChaveFmp`, a fonte é **pulada** (não é tratada como falha) e o startup registra
um `warning` dizendo qual credencial falta. O Banco Central não exige credencial, então
`api/mercado/indicadores` funciona em qualquer ambiente.

---

## 3. Endpoints

| Rota | Resposta |
|---|---|
| `GET api/mercado/cotacoes/{ticker}` | `CotacaoResponse` (`200`) |
| `GET api/mercado/fundamentos/{ticker}` | `FundamentoResponse` (`200`); `cobertura` = `Completa` ou `Parcial` |
| `GET api/mercado/indicadores` | `IndicadoresResponse` (`indicadores` + `avisos`) |

### 3.1 Status

| Status | Quando |
|---|---|
| `200` | a fonte respondeu com dado |
| `400` | ticker vazio ou com mais de 20 caracteres |
| `404` | todas as fontes disponíveis responderam que **não existe** |
| `429` | mais de 60 consultas por minuto do mesmo IP (com `Retry-After`) |
| `503` | nenhuma fonte disponível respondeu (**não é** o mesmo que "não existe") |

O `503` é deliberado: devolver `404` quando a fonte está fora do ar afirmaria algo falso sobre o
ticker. As credenciais viajam na query string das APIs, então nenhuma mensagem de erro ou log
reproduz a URL.

### 3.2 Cobertura de fundamentos

| Valor | Significado |
|---|---|
| `Completa` | perfil + múltiplos (FMP) |
| `Parcial` | só múltiplos publicados (BRAPI: P/L, LPA, nome, moeda) |

O valor padrão do enum é `Parcial`: um provedor novo que esqueça de declarar a cobertura nunca
superestima o dado.

---

## 4. Verificação rápida

```bash
curl -s http://localhost:5000/api/mercado/indicadores
curl -s http://localhost:5000/api/mercado/cotacoes/PETR4
curl -s http://localhost:5000/api/mercado/fundamentos/AAPL
```

Sem credencial, os dois últimos respondem `503` com a instrução do que configurar — comportamento
esperado, não defeito.

---

## 5. Testes

`tests/Portfy.Tests/Integracoes/` cobre: mapeamento de cada payload, aliases da FMP (v3/stable),
404 × 503, fallback de cotação e de fundamentos, cobertura parcial, cache, disjuntor e o limite de
60 consultas por minuto. Nenhum teste toca a rede (`HttpMessageHandler` falso).
