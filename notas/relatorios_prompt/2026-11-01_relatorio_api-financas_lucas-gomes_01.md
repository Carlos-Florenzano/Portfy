# Relatório — Integração de APIs Financeiras (FMP, BRAPI e Banco Central/SGS)

| Item | Valor |
|---|---|
| Repositório | `C:\Users\Lucas\Desktop\projetos\portfy` |
| Prompt de origem | `notas/prompts/2026-11-01-prompt_api_financas_Lucas-gomes-01.md` |
| Data | 2026-11-01 |
| Escopo | Integrar três fontes de dados financeiros (FMP, BRAPI, BCB/SGS) de forma complementar, com fallback e contratos padronizados |
| VCS | Nenhum commit, merge, push ou tag foi executado; nenhuma operação de git foi feita pelo agente |

---

## 1. Contexto lido antes de alterar código

`README.md`, `docs/arch.md` (§1 a §22), `docs/funcionalidades.md`, `docs/diagrama.md`,
`docs/diagrama-classe-candidatas.md`,
`notas/importante/sugestoes_mvp_portfy_temp_outubro_2026.md`, e todo o backend
(`Program.cs`, `Controllers/`, `Dtos/`, `Services/`, `Models/`, `Data/`), além da suíte
`tests/Portfy.Tests`.

O `docs/arch.md` §2.4 declarava "cotação em tempo real por API externa" fora do escopo do MVP.
A autorização explícita do dono do produto está no prompt de origem; por isso a exceção foi
registrada como **ADR-020** em `docs/arch.md` §19 e a nota de escopo foi atualizada em §2.4.
O roadmap do projeto **não** foi alterado: o arquivo é protegido pelo pipeline de SDD.

### Decisões

| # | Decisão | Motivo |
|---|---|---|
| 1 | BRAPI é preferida para ticker da B3 (`^[A-Z]{4}[0-9]{1,2}$`, ex.: `PETR4`, `HGLG11`); FMP para o restante (`AAPL`, ETFs globais) | divisão de responsabilidades pedida no prompt, com regra determinística em vez de heurística |
| 2 | Banco Central (SGS) é a única fonte de macroeconomia | é fonte primária e oficial; não existe substituto confiável para Selic/IPCA/CDI/câmbio |
| 3 | Fundamentos vêm só da FMP (sem fallback) | a BRAPI não cobre fundamentos profundos; um "fallback" de 2 campos seria degradação silenciosa |
| 4 | Contrato padronizado em `Dtos/Mercado/` + interfaces em `Services/Contracts/` | o consumidor usa `IMercadoService` e não conhece FMP, BRAPI nem SGS |
| 5 | Nenhum pacote NuGet novo | `HttpClient` e `IMemoryCache` já fazem parte do framework compartilhado (arch §5.3) |
| 6 | Credenciais só por ambiente (`Integracoes__TokenBrapi`, `Integracoes__ChaveFmp`) | arch §15.5: segredo nunca no repositório |
| 7 | O arquivo de configuração da aplicação (`appsettings`) não foi alterado | ele é protegido pelo boundary de escrita do agente; os defaults ficam em `OpcoesDeIntegracao` e as credenciais entram por ambiente |

---

## 2. Arquitetura da integração

```
Controllers/MercadoController.cs       (borda HTTP, 3 rotas de leitura)
        │
        ▼
IMercadoService  ◄── Services/Integracoes/MercadoService.cs (orquestrador)
        │                 │  cache (IMemoryCache)
        │                 │  disjuntor por fonte
        │                 │  ordem/fallback por tipo de ticker
        ▼                 ▼
IProvedorDeCotacoes    IProvedorDeFundamentos    IProvedorDeIndicadores
   ├── ProvedorBrapi      └── ProvedorFmp          └── ProvedorBcb
   └── ProvedorFmp
        │                     │                        │
        ▼                     ▼                        ▼
   brapi.dev         financialmodelingprep.com    api.bcb.gov.br (SGS)
```

### 2.1 Contratos padronizados

| Contrato | Arquivo | Papel |
|---|---|---|
| `CotacaoResponse` | `Dtos/Mercado/CotacaoResponse.cs` | ticker, nome, preço, variação, moeda, atualização e `Fonte` |
| `FundamentoResponse` | `Dtos/Mercado/FundamentoResponse.cs` | setor, indústria, país, moeda, market cap, P/L, LPA, beta, descrição |
| `IndicadorResponse` | `Dtos/Mercado/IndicadorResponse.cs` | código, descrição, valor, data, unidade e série SGS |
| `IndicadoresResponse` | `Dtos/Mercado/IndicadoresResponse.cs` | coleção de indicadores + `Avisos` do que a fonte não entregou |
| `OrigemDeMercado` | `Dtos/Mercado/OrigemDeMercado.cs` | identidade da fonte usada para rotear **e** para preencher `Fonte` |
| `IProvedorDeCotacoes` / `IProvedorDeFundamentos` / `IProvedorDeIndicadores` / `IMercadoService` | `Services/Contracts/` | contratos injetados (ADR-013): nenhum controller conhece implementação |
| `ServicoExternoIndisponivelException` | `Services/Contracts/` | falha de fonte externa → `503` no middleware único (ADR-005) |
| `IRelogio` | `Services/Contracts/` + `Services/RelogioDoSistema.cs` | arch §16.1, item 7: nada lê o relógio do sistema diretamente |

`VariacaoPercentual`, `CapitalizacaoDeMercado`, `PrecoLucro`, `LucroPorAcao`, `Beta` e
`AtualizadaEm` são nulos quando a fonte não informa o dado: ausência nunca vira zero.

### 2.2 Resiliência (o que o prompt pediu em §4.1)

| Mecanismo | Onde | Efeito |
|---|---|---|
| Fallback por tipo de ticker | `MercadoService.OrdemDeProvedores` | BRAPI fora do ar → a mesma cotação é tentada na FMP |
| Cache com TTL por recurso | `MercadoService` + `OpcoesDeIntegracao` | cotação 60 s, fundamento 12 h, indicador 1 h: protege a cota diária do plano Basic da FMP |
| Disjuntor por fonte | `Services/Integracoes/DisjuntorDeProvedores.cs` | fonte que falhou (429/5xx/timeout/credencial) sai da rotação por 60 s; o fallback fica imediato |
| Timeout e `CancellationToken` | `RegistroDeIntegracoes` + `ProvedorHttpBase` | `Integracoes:SegundosDeTimeout` (8 s); cancelamento do cliente não é confundido com falha do provedor |
| Distinção 404 × 503 | `MercadoService` + `ExceptionHandlingMiddleware` | "não perguntei" (`503`) nunca é reportado como "não existe" (`404`) |
| Degradação parcial no SGS | `MercadoService.ObterIndicadoresAsync` | uma série com problema não esconde as demais; o que falhou vai em `Avisos` |
| Fonte não configurada ≠ falha | `EstaConfigurado` em cada provedor | sem token/chave, a fonte é pulada e a API continua no ar |

### 2.3 Endpoints

| Rota | Fonte | Resposta |
|---|---|---|
| `GET api/mercado/cotacoes/{ticker}` | BRAPI (B3) ou FMP (global), com fallback | `CotacaoResponse` (`200`), `404` se nenhuma fonte conhece o ticker, `503` se nenhuma fonte está disponível |
| `GET api/mercado/fundamentos/{ticker}` | FMP | `FundamentoResponse`, `404`/`503` nas mesmas condições |
| `GET api/mercado/indicadores` | BCB/SGS (433 IPCA, 432 Selic, 4389 CDI, 1 Dólar) | `IndicadoresResponse` com `avisos` das séries que falharam |

Séries do SGS confirmadas contra a API oficial em 2026-11-01:
`433` IPCA (% no mês), `432` Selic acumulada no mês, `4389` CDI acumulado no mês,
`1` dólar comercial (venda). Payload real do SGS: `[{"data":"01/09/2026","valor":"0.82"}]`.

### 2.4 Segurança

1. Credencial nunca aparece em mensagem de erro: `ProvedorHttpBase` monta a exceção com status
   e texto fixos, sem a URL que carrega `?token=`/`?apikey=`.
2. Log do `HttpClient` rebaixado para `Warning` em `Program.cs`, porque o handler padrão registra
   a URL completa em `Information`.
3. Nenhuma chave no repositório: `OpcoesDeIntegracao` tem default vazio e a credencial vem de
   variável de ambiente.
4. Campo que a fonte não envia fica nulo; não há valor "chutado" (nem `1.0` de câmbio, arch §11.8).

---

## 3. Arquivos

### 3.1 Criados

| Arquivo | Papel |
|---|---|
| `Dtos/Mercado/CotacaoResponse.cs` | contrato de cotação |
| `Dtos/Mercado/FundamentoResponse.cs` | contrato de fundamentos |
| `Dtos/Mercado/IndicadorResponse.cs` | contrato de indicador |
| `Dtos/Mercado/IndicadoresResponse.cs` | coleção + avisos |
| `Dtos/Mercado/OrigemDeMercado.cs` | enum de origem/fonte |
| `Services/Contracts/IProvedorDeCotacoes.cs` | contrato de cotação |
| `Services/Contracts/IProvedorDeFundamentos.cs` | contrato de fundamentos |
| `Services/Contracts/IProvedorDeIndicadores.cs` | contrato de indicadores |
| `Services/Contracts/IMercadoService.cs` | fronteira consumida pela aplicação |
| `Services/Contracts/IndicadorEconomico.cs` | indicadores suportados |
| `Services/Contracts/IRelogio.cs` | abstração de tempo |
| `Services/Contracts/ServicoExternoIndisponivelException.cs` | falha externa → 503 |
| `Services/RelogioDoSistema.cs` | implementação de produção do relógio |
| `Services/Integracoes/OpcoesDeIntegracao.cs` | configuração das fontes |
| `Services/Integracoes/ProvedorHttpBase.cs` | transporte + tradução de erro, sem vazar URL |
| `Services/Integracoes/LeitorJson.cs` | leitura tolerante (aliases v3/stable da FMP) |
| `Services/Integracoes/DisjuntorDeProvedores.cs` | disjuntor por fonte |
| `Services/Integracoes/ProvedorBrapi.cs` | adaptador BRAPI |
| `Services/Integracoes/ProvedorFmp.cs` | adaptador FMP (cotação + fundamentos) |
| `Services/Integracoes/ProvedorBcb.cs` | adaptador SGS |
| `Services/Integracoes/MercadoService.cs` | orquestração, cache, fallback e disjuntor |
| `Services/Integracoes/RegistroDeIntegracoes.cs` | registro de DI (chamado por `Program.cs`) |
| `Controllers/MercadoController.cs` | borda HTTP das três rotas |
| `tests/Portfy.Tests/Integracoes/Dubles.cs` | manipulador HTTP falso, relógio falso e provedores falsos |
| `tests/Portfy.Tests/Integracoes/ProvedorBrapiTests.cs` | 6 testes |
| `tests/Portfy.Tests/Integracoes/ProvedorFmpTests.cs` | 7 testes |
| `tests/Portfy.Tests/Integracoes/ProvedorBcbTests.cs` | 6 testes (+4 casos de teoria) |
| `tests/Portfy.Tests/Integracoes/MercadoServiceTests.cs` | 16 testes |
| `notas/relatorios_prompt/2026-11-01_relatorio_api-financas_lucas-gomes_01.md` | este relatório |

### 3.2 Alterados

| Arquivo | Alteração |
|---|---|
| `Program.cs` | `AdicionarIntegracoesDeMercado(builder.Configuration)` + filtro de log do `HttpClient` |
| `Middleware/ExceptionHandlingMiddleware.cs` | `ServicoExternoIndisponivelException` → `503` com `ProblemDetails` |
| `tests/Portfy.Tests/Middleware/ExceptionHandlingMiddlewareTests.cs` | caso do `503` |
| `tests/Portfy.Tests/Api/ApiFactory.cs` | `Pooling=False` no banco de teste (ver §6) |
| `docs/arch.md` | ADR-020, §2.4, §8.1 (árvore), §18.2 (gate refinado), §20 (P2-h), contagem de testes |

`Models/`, `Data/` e as migrations **não** foram tocados: a integração não persiste nada.

---

## 4. Gates executados

O shell do agente não herdava as variáveis de pasta do Windows (`PROGRAMFILES` e cia.), e o
NuGet falhava com `Value cannot be null (Parameter 'path1')` em **qualquer** projeto. Com as
variáveis exportadas, os gates rodaram normalmente:

```
env APPDATA=... LOCALAPPDATA=... PROGRAMDATA=... ALLUSERSPROFILE=... PROGRAMFILES='C:\Program Files' \
    'PROGRAMFILES(X86)=C:\Program Files (x86)' dotnet build Portfy.csproj
→ Compilação com êxito. 0 Aviso(s), 0 Erro(s)

env (mesmas variáveis) dotnet test tests/Portfy.Tests
→ Aprovado! Com falha: 0, Aprovado: 140, Ignorado: 0, Total: 140
```

### 4.1 Verificações estáticas (arch §18.2)

| Regra | Comando | Resultado |
|---|---|---|
| Sem setter público em modelos | `grep -rn "get; set;" Models` | ✅ vazio (não alterado) |
| Sem campo estático mutável | `grep -rnP ... 'private static (?!readonly)... _[[:alnum:]_]+[[:space:]]*[=;]' Controllers Services` | ✅ vazio |
| Sem `EnsureCreated` | `grep -rn "\.EnsureCreated(" --include="*.cs" .` | ✅ vazio |
| Sem `Console.` em `Services` | `grep -rn "Console\." Services` | ❌ pré-existente (`RelatorioFinanceiro.cs`, P2-a) |
| Sem `DateTime.Now` | `grep -rn "DateTime.Now" --include="*.cs" Models Services Controllers Data` | ❌ pré-existente (3 em `Models/`, P2-d) |

As duas linhas vermelhas já estavam documentadas como dívida em `docs/arch.md` §20 e **não**
foram introduzidas por esta mudança; a integração nova usa `IRelogio` (`DateTimeOffset.UtcNow`
em um único ponto) e não escreve em console.

### 4.2 Smoke real (arch §18.1, "endpoint novo")

API rodando em `http://127.0.0.1:5199` com o banco local, sem credenciais de terceiros:

| Requisição | Resultado |
|---|---|
| `GET /api/mercado/indicadores` | `200` com as 4 séries reais do BCB: IPCA `0.82` (2026-09-01), Selic `13.75`, CDI `13.65`, Dólar `5.4212`, `avisos: []` |
| `GET /api/mercado/cotacoes/PETR4` | `503` `ProblemDetails`: "Nenhuma fonte de cotação disponível respondeu para 'PETR4'." |
| `GET /api/mercado/fundamentos/AAPL` | `503` `ProblemDetails`: "A fonte de fundamentos não está configurada: defina Integracoes:ChaveFmp." |
| `GET /api/mercado/cotacoes/` (ticker vazio) | `400` de validação do roteamento |

O `503` sem credencial é o comportamento esperado: nenhuma fonte configurada, nenhuma cotação
inventada. Com `Integracoes__TokenBrapi`/`Integracoes__ChaveFmp` definidas, as rotas passam a
consultar as fontes reais.

---

## 5. Como rodar com credenciais

```bash
export Integracoes__TokenBrapi='seu-token-brapi'
export Integracoes__ChaveFmp='sua-chave-fmp'
dotnet run --project Portfy.csproj
```

Sem credencial: `api/mercado/indicadores` continua funcionando (BCB é aberto), e as rotas de
cotação/fundamentos respondem `503` explicando o que falta. As demais rotas da API não mudam.

Limites do plano Basic da FMP (cota diária) são acomodados por cache de 60 s (cotação) e 12 h
(fundamento), mas não há contador local de cota: se o teto diário estourar, a resposta da fonte
vira `429`, o disjuntor abre e o fallback assume. Um orçamento diário explícito entrou como
dívida P2-h.

---

## 6. Falha pré-existente corrigida no caminho

O primeiro `dotnet test` completo reprovou **5 de 140** testes, todos em
`tests/Portfy.Tests/Api/AutenticacaoTests`, com
`System.IO.IOException: The process cannot access the file 'portfy-tests-*.db' because it is
being used by another process` em `ApiFactory.Dispose`.

Evidência de que a falha não vinha desta mudança:

1. Rodando só `--filter "FullyQualifiedName~AutenticacaoTests"`, o resultado foi **5/5 falhas**
   — nenhum arquivo da integração participa desses testes.
2. O caminho da exceção é `ApiFactory.Dispose` → `File.Delete` do banco temporário.

Causa raiz: o pool de conexões do `Microsoft.Data.Sqlite` mantém o arquivo aberto depois do
`Dispose`, e no Windows o `File.Delete` falha enquanto existe handle. Correção mínima no seam de
teste (`ApiFactory`): `Pooling=False` na connection string do banco temporário. Depois disso:
**140/140 aprovados**. Nenhum código de produção foi alterado por causa disso e a falha não é
declarada como regressão da integração.

---

## 7. Pendências e limitações

| # | Item | Situação |
|---|---|---|
| 1 | `Integracoes__TokenBrapi` e `Integracoes__ChaveFmp` precisam ser definidas no ambiente | sem elas, cotação e fundamentos respondem `503` (por desenho) |
| 2 | Endpoints `api/mercado` são públicos | consomem cota de terceiro; limite por IP registrado como P2-h em `docs/arch.md` §20 |
| 3 | Fundamentos não têm fallback | a BRAPI não cobre fundamentos profundos; `503`/`404` é a resposta honesta |
| 4 | Nomes de campo da FMP não puderam ser verificados com chave real | o adaptador lê por aliases (`price`/`regularMarketPrice`, `changesPercentage`/`changePercentage`, `pe`/`peRatio`, `eps`/`epsTtm`) e trata objeto de erro como fonte indisponível; `ProvedorFmpTests` cobre os dois formatos |
| 5 | Contador local de cota diária da FMP | não implementado; cache + disjuntor + 429→fallback cobrem o caso (P2-h) |
| 6 | SPA não consome os endpoints novos | nenhum tipo espelho em `frontend/src/types` foi criado (não há tela consumindo); o espelho entra junto da tela, como manda o arch §10.7 |
| 7 | `appsettings` e o roadmap do projeto | não alterados: são protegidos pelo boundary do agente nesta execução |

### 7.1 Recomendação de próximo passo

Usar `GET api/mercado/indicadores` no card de saúde financeira do Dashboard e
`GET api/mercado/cotacoes/{ticker}` no componente de ativos, espelhando os DTOs em
`frontend/src/types/` no mesmo commit da tela.
