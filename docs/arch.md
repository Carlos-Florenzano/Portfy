# ARCH — Princípios de Construção de Software do Portfy

**Versão:** 1.1
**Escopo de autoridade:** este documento é a fonte de verdade para **como** o Portfy é construído (escopo de MVP, camadas, estrutura de pastas, regras de dependência, convenções, gates). Ele consome — e não substitui — as fontes listadas em §2.
**Aplicação:** obrigatório para qualquer alteração de código, estrutura ou dependência. Toda exceção exige uma entrada em [ADR](#19-decisões-vinculantes).
**Público-alvo primário:** agentes de IA e desenvolvedores que alteram o repositório sem contexto prévio.

---

## 1. Diretriz de projeto

O Portfy será desenvolvido e evoluído majoritariamente por agentes de IA. Toda decisão de construção deve maximizar: **clareza explícita, rastreabilidade, determinismo e verificabilidade por comando**, e minimizar dependência de contexto tácito.

Consequências práticas:

1. Prefira o explícito ao elegante. Um arquivo a mais é preferível a uma convenção implícita.
2. Toda regra de negócio tem **um único dono** identificável por caminho de arquivo.
3. Todo estado persistido tem **uma única autoridade** (§9).
4. Toda afirmação de "pronto" é verificável por um comando que falha quando a afirmação é falsa (§18).

---

## 2. Fontes de requisitos e escopo do MVP

### 2.1 Autoridade por documento

| Documento | É autoridade sobre | Quando é obrigatório ler |
|---|---|---|
| `docs/funcionalidades.md` | **escopo funcional do MVP** (o *quê*) | antes de criar/alterar qualquer funcionalidade |
| `docs/portfy_avaliacao_av1_professor_andre.md` | **critérios de qualidade OO avaliados** e causas raiz apontadas | antes de alterar domínio, camadas ou estrutura |
| `docs/AVISOS.txt` | **procedimento operacional** de migrations EF | antes de mexer em schema |
| `README.md` | visão geral, integrantes, pendências declaradas | ao iniciar trabalho no repositório |
| `docs/arch.md` (este) | **como** construir | sempre |

Regra: se uma alteração contraria `funcionalidades.md`, ela está fora de escopo. Se contraria este documento, exige ADR.

### 2.2 Escopo funcional do MVP (`funcionalidades.md` §3) → módulo dono

| Funcionalidade | Módulo dono canônico | Estado atual | Delta arquitetural exigido |
|---|---|---|---|
| Rebalanceamento automático de aportes (§3.1) | `Services/RebalanceamentoService.cs` | **inexistente** | `AlocacaoMeta` + cálculo puro determinístico (§11.5) |
| Conversão de moedas simplificada (§3.1) | `Services/CambioService.cs` + `Models/CotacaoCambio.cs` | **inexistente**; `Ativo` não tem país/moeda | adicionar `Pais`/`Moeda` a `Ativo` + tabela de câmbio de atualização manual (ADR-015) |
| Pesquisa de ativos por ticker/nome (§3.2) | `Services/AtivoService.cs` + `AtivoController` | **parcial**: catálogo `static` em memória, sem busca | busca sobre `DbSet<Ativo>`; remover catálogo em memória (§9) |
| Seed inicial offline de ativos (§3.2) | `Data/Seed/` executado por migration | **inexistente** | seed idempotente (ADR-016) |
| Visão consolidada macro/micro, gráfico de pizza (§3.3) | `Services/RelatorioFinanceiroService.cs` → DTO; renderização no SPA | **parcial**: hoje imprime em console | relatório vira DTO; `Console` sai do `Services` (§6.2) |
| Simulador "e se?" (§3.3) | endpoint de *preview* que não persiste | **inexistente** | `POST api/carteiras/{id}/simular` sem escrita (ADR-017) |
| Orçamentos fixos vs. variáveis, contas recorrentes (§3.4) | `Models/Orcamento.cs` (existe) | **parcial** | falta classificar fixo/variável e vincular a usuário |
| Indicador de taxa de poupança (§3.4) | `Services/IndicadorService.cs` | **inexistente** | fórmula única, determinística, testada |
| Múltiplas simulações + seletor de perfil (§3.5) | `CarteiraSimulada` com `Id`, `UsuarioId`, `Nome` | **não existe** (hoje 1 carteira estática) | modelagem por usuário; remover singleton (§9) |
| Clonagem de simulação (§3.5) | `Services/CarteiraService.ClonarAsync` | **inexistente** | cópia sem IDs, em transação única |

### 2.3 Schema proposto em `funcionalidades.md` §2 → modelo canônico

| Proposta da spec | Modelo canônico | Observação |
|---|---|---|
| `simulations` | `CarteiraSimulada` + `Id`, `UsuarioId`, `Nome` | mantém-se o nome da classe (evita churn); "simulação" é o nome de produto de uma carteira |
| `assets` | `Ativo` + subclasses (`Acao`, `Fii`, `Etf`, `Cripto`, `RendaFixa`) | faltam `Pais` e `Moeda` para ativos globais |
| `portfolio_allocations` | `AlocacaoMeta` (**nova**) | percentual-alvo por ativo/carteira; validada por §11, item 4 |
| `expenses` | `Orcamento` (existe) | no MVP o gasto é **acumulador** (`AdicionarDespesa`); lançamento datado (`Despesa`) fica adiado (ADR-014) |

### 2.4 Fora do escopo do MVP

Cotação em tempo real por API externa, câmbio automático, histórico de preços, autenticação social/2FA, notificações, deploy e CI. Só entram com funcionalidade escrita em `funcionalidades.md`.

---

## 3. Conformidade com a avaliação AV1

A avaliação (`docs/portfy_avaliacao_av1_professor_andre.md`) é um **sinal de engenharia**, não um artefato decorativo: cada dimensão com nota baixa indica uma regra estrutural ausente. Nota final 6,92 (POO 6,45 · Frontend 8,00).

### 3.1 Dimensão → requisito arquitetural → gate

| Dim. | Nota | Causa raiz apontada | Requisito neste documento | Gate |
|---|---|---|---|---|
| A — Modelagem | 7,0 | domínio existe, mas sem simulação por usuário/alocação | §9.1 | R2 |
| B — Estado/construtor | 9,0 | **ok** — construtores garantem estado inicial | manter: §16.1, item 3 | testes de construtor |
| C — Encapsulamento | 9,0 | **ok** — `private set`, coleções como `IReadOnlyCollection` | manter: §16.1, itens 1-2 | `grep` (§18.2) |
| D — Responsabilidades/coesão | 5,5 | `UI/Dashboard.cs` (701 linhas) + serviço que imprime em console | §6.1, §6.2, R6 | `grep Console` |
| E — Colaboração entre objetos | 8,0 | **ok** — composição × associação corretas | §9.2 | revisão |
| F — Abstração | 2,0 | nenhuma interface; estado global sem DI | §3.2 (ADR-013) | R6 |
| G — Polimorfismo | 2,0 | resolvido depois da AV1 (`Ativo` abstrato + 5 subclasses, commit `4a8be0a`) | §16.1, item 6 | testes de subtipo |
| H — Testes das regras | 0,0 | zero testes | §18.1 + P1-b | `dotnet test` |
| I — CRUD funcional | 9,0 | **ok** — CRUD completo e rotulado | manter | smoke |
| J — Integração front↔back | 7,0 | sem sessão/token; contrato não espelhado | §10; §15, item 4 | R4 |
| K — Organização frontend | 7,0 | `src/` plano, tela e CSS misturados | §8.2 | `npm run build` |

### 3.2 Como fechar "Abstração" sem inflar a arquitetura

A AV1 sugere "uma interface para a carteira". A decisão deste documento (ADR-013):

1. A abstração exigida é **contrato de serviço injetado**, não camada de repositório. `DbContext` já é a abstração de persistência; `IRepository<T>` genérico é abstração especulativa e fica proibido.
2. Interfaces legítimas (cada uma com dublê real de teste ou segunda implementação):
   - `ICarteiraService` — consumida pelo `CarteiraController`; permite testar o controller sem banco.
   - `IClock` — remove `DateTime.Now/UtcNow` de dentro do domínio; torna data determinística.
   - `IGeradorDeVariacao` — remove aleatoriedade do domínio (§11, item 6).
   - `ISenhaHasher` — hash de senha fora do modelo (§13, item 5).
3. Interface sem consumidor que a injete é proibida. Criar `IFooService` e registrar em DI sem uso é o mesmo que não ter abstração.

### 3.3 Pendências herdadas da AV1 (estado em 2026-10-28)

| Item da AV1 | Situação |
|---|---|
| Remover `UI/Dashboard.cs` e a pasta `UI/` | ✅ **resolvido** (commit `cbe4c05`) |
| Introduzir herança/polimorfismo em `Ativo` | ✅ **resolvido** (commit `4a8be0a`) |
| Introduzir DI + interface | ❌ pendente (R6) |
| Escrever testes das invariantes | ❌ pendente (P1-b) |
| Trocar `alert`/`console.error` por erro em tela + interceptor | ❌ pendente (P1-f) |
| Resíduo de console no backend | ⚠️ `Services/RelatorioFinanceiro.cs` ainda imprime (`Console.Clear`/`WriteLine`) |

---

## 4. Estado atual verificado (baseline)

Medido em 2026-10-28. **Não é opinião — é o resultado dos comandos abaixo.**

| Item | Comando / evidência | Resultado |
|---|---|---|
| Build do backend | `dotnet build Portfy.csproj` | **FALHA — 9 erros**, todos em `Controllers/AtivoController.cs` |
| Build do frontend | `cd frontend && npm run build` | **PASSA** (79 módulos, ~250 ms) |
| Lint do frontend | `cd frontend && npm run lint` | **FALHA — 3 erros** `@typescript-eslint/no-explicit-any` (`Cadastro.tsx:51`, `Dashboard.tsx:37`, `Login.tsx:41`) |
| Migrations EF | `ls Data/` | **inexistentes** (só `AppDbContext.cs`) |
| Projeto de testes | busca por `*Tests*` | **inexistente** |
| `AppDbContext` em uso | busca por `AppDbContext` em `*.cs` | **nenhum controller consome o banco** |
| Autenticação | busca por `AddAuthentication` / `[Authorize]` | **inexistente** |
| `get; set;` em `Models/` | `grep -rn "get; set;" Models` | **vazio** ✅ (conquista da AV1, preservar) |
| `alert` no frontend | `grep -rn "alert(" frontend/src` | 5 ocorrências (`Dashboard.tsx:62,64,79,82`, `Cadastro.tsx:49`) |
| Solution file / Docker / CI | busca por `*.sln`, `Dockerfile*` | **inexistentes** |

### 4.1 Erros de build (bloqueadores atuais)

Todos em `Controllers/AtivoController.cs`, escrito contra a versão antiga de `Ativo`:

| Linha(s) | Erro | Causa raiz |
|---|---|---|
| 18–21 | `CS0144` — não é possível instanciar tipo abstrato `Ativo` | `Ativo` virou abstrato (`Models/Ativos/Ativo.cs:6`) e o controller faz `new Ativo(...)` |
| 41, 63, 97 | `CS1061` — `Ativo` não contém `RentabilidadeSimulada` | a propriedade hoje se chama `RentabilidadeAcumulada` |
| 84, 89 | `CS1061` — `Ativo` não contém `SimularVariacaoPreco` | o método hoje se chama `AtualizarPreco(decimal)` |

**Consequência normativa:** build e lint são gates independentes e ambos estão vermelhos. Nenhuma feature nova antes de R1 e P1-f.

### 4.2 Três autoridades de estado concorrentes

| Onde | Estado guardado | Durabilidade |
|---|---|---|
| `Controllers/UsuarioController.cs:9` (`private static List<Usuario>`) | usuários, hash de senha | memória do processo |
| `Controllers/CarteiraController.cs:31` (`private static CarteiraSimulada`) | saldo, posições, aportes | memória do processo |
| `Program.cs:9` (`AddDbContext` + SQLite) | declarado, **não consumido** | arquivo `portfy.db` |

Viola a regra de dono único (§9) — dívida **P0-a**.

---

## 5. Stack canônica

Versões verificadas nos arquivos do repositório. **Trocar qualquer item exige ADR.**

### 5.1 Backend

| Camada | Tecnologia | Versão | Fonte |
|---|---|---|---|
| Runtime / SDK | .NET | `net10.0` / SDK 10.0.402 | `Portfy.csproj:4` |
| Framework web | ASP.NET Core Web API | 10 | `Portfy.csproj:1` |
| ORM | EF Core + provider SQLite | 10.0.13 | `Portfy.csproj:11-12` |
| Tooling EF | `Microsoft.EntityFrameworkCore.Design` | 10.0.13 | `Portfy.csproj:7` |
| Documentação de API | Swashbuckle (Swagger UI, só Development) | 6.5.0 | `Portfy.csproj:13` |
| Banco | SQLite (`portfy.db`) | — | `appsettings.json:8` |
| Análise estática | `Nullable` + `ImplicitUsings` | — | `Portfy.csproj:5-6` |

### 5.2 Frontend

| Camada | Tecnologia | Versão |
|---|---|---|
| UI | React + react-dom | 19.2.8 |
| Linguagem | TypeScript | ~6.0.2 |
| Build / dev server | Vite + `@vitejs/plugin-react` | 8.3.0 / 6.1.1 |
| HTTP client | axios (instância única) | 1.20.0 |
| Estilo | CSS puro, um arquivo por tela | — |
| Lint | ESLint 10 + typescript-eslint 8.69 | — |
| Roteamento | **nenhum** (navegação por `useState` em `src/App.tsx`) | — |

### 5.3 Regras de stack

1. **Proibido** adicionar dependência que não seja exigida por funcionalidade já descrita em `docs/funcionalidades.md`. Gráficos (`Chart.js`/`Recharts`), router, state manager: só quando a feature existir, com ADR (§2.2 — gráfico de pizza é o primeiro candidato legítimo).
2. **Proibido** `float`/`double` para dinheiro (§11, item 1).
3. **Proibido** um segundo ORM, segundo cliente HTTP ou segundo framework CSS.
4. Versões ficam fixadas no manifesto (`Portfy.csproj`, `package.json`). Sem `latest`.

---

## 6. Camadas

```
┌──────────────────────────────────────────────────────────────┐
│ Borda HTTP           Controllers/            HTTP, status, DTO│
├──────────────────────────────────────────────────────────────┤
│ Contratos            Dtos/                   forma do payload │
├──────────────────────────────────────────────────────────────┤
│ Aplicação            Services/               regras que cruzam │
│                                              entidades ou I/O  │
├──────────────────────────────────────────────────────────────┤
│ Domínio              Models/                 invariantes,      │
│                                              cálculo financeiro│
├──────────────────────────────────────────────────────────────┤
│ Persistência         Data/                   mapeamento EF     │
└──────────────────────────────────────────────────────────────┘
        Frontend (SPA) ──HTTP/JSON──► Borda HTTP
```

### 6.1 Responsabilidade única por pasta

| Pasta | Uma única responsabilidade | Motivo de mudança |
|---|---|---|
| `Controllers/` | traduzir HTTP ↔ chamadas de `Services`; decidir status code | muda o contrato de rota/verbo |
| `Dtos/` | descrever o formato de entrada e saída | muda o payload |
| `Services/` | orquestrar regra de negócio e persistência | muda a regra de negócio |
| `Models/` | proteger invariantes do domínio | muda a regra intrínseca da entidade |
| `Data/` | mapear modelo relacional e configurações EF | muda o schema |
| `Middleware/` | política transversal de erro | muda a política de erro |

### 6.2 Proibições explícitas (violações já observadas)

| Proibição | Onde é violado hoje |
|---|---|
| Controller devolver objeto anônimo (`new { ... }`) | todos os controllers |
| Controller conter DTO aninhado | `UsuarioController.cs:13-38`, `AtivoController.cs:24-27`, `AporteController.cs:26-30` |
| Controller duplicar `try/catch (ArgumentException)` | `UsuarioController.cs` (4 blocos) |
| Controller/Service guardar estado em `static` mutável | `UsuarioController.cs:9`, `CarteiraController.cs:31`, `AtivoController.cs:15`, `AporteController.cs:24` |
| `Services/` escrever em `Console` | `Services/RelatorioFinanceiro.cs` |
| Código morto / duplicado | `Models/Posicao.cs` está **inteiro comentado**; a classe real vive em `Models/CarteiraSimulada.cs` |
| Duas classes públicas no mesmo arquivo | `Posicao` dentro de `CarteiraSimulada.cs` |
| Pasta ≠ namespace | `Models/Ativos/*.cs` declaram `namespace Portfy.Models` |
| Frontend usar `any` | `Cadastro.tsx:51`, `Dashboard.tsx:37`, `Login.tsx:41` |
| Frontend tratar erro com `alert`/`console.error` | `Dashboard.tsx:62-83`, `Cadastro.tsx:49`, `components/Ativos.tsx:26` |
| Frontend calcular valor financeiro autoritativo | `Dashboard.tsx:52-59` recalcula `salarioMensal` localmente |

---

## 7. Regras de dependência

Uma seta só existe se estiver nesta tabela.

### 7.1 Backend

| Origem ↓ / pode depender de → | `Controllers` | `Dtos` | `Services` | `Models` | `Data` |
|---|---|---|---|---|---|
| `Controllers` | — | ✅ | ✅ | ✅ leitura | ❌ |
| `Dtos` | ❌ | — | ❌ | ❌ | ❌ |
| `Services` | ❌ | ✅ | — | ✅ | ✅ |
| `Models` | ❌ | ❌ | ❌ | — | ❌ |
| `Data` | ❌ | ❌ | ❌ | ✅ | — |

Regras derivadas:

1. `Controllers` **não** referenciam `AppDbContext`, `Microsoft.EntityFrameworkCore` nem connection string.
2. `Models` não referenciam EF Core, ASP.NET Core, `Console`, `DateTime.Now` ou geradores aleatórios.
3. `Services` não retornam `IActionResult`, não conhecem status HTTP e não escrevem em `Console`.
4. `Dtos` são tipos de dados puros; `DataAnnotations` permitido, lógica proibida.
5. Dependência cíclica entre pastas é sempre erro.
6. `Program.cs` é o **único** ponto de composição: registro de DI, pipeline, CORS, JSON.

### 7.2 Frontend

| Origem ↓ / pode depender de → | `pages` | `components` | `services` | `types` |
|---|---|---|---|---|
| `pages` | — | ✅ | ✅ | ✅ |
| `components` | ❌ | ✅ | ✅ | ✅ |
| `services` | ❌ | ❌ | ✅ | ✅ |
| `types` | ❌ | ❌ | ❌ | — |

1. `axios` é importado **exclusivamente** em `src/services/http.ts`.
2. `pages` e `components` não chamam `http` diretamente — sempre via `services`.
3. `types` contém apenas `type`/`interface`.

---

## 8. Estrutura de pastas canônica

### 8.1 Backend (raiz do repositório)

```
Portfy/
├── Portfy.csproj                  # manifesto: TFM, pacotes, exclusões de build
├── Portfy.sln                     # criar quando existir o projeto de testes
├── Program.cs                     # ÚNICO ponto de composição
├── appsettings.json               # configuração não-secreta
├── appsettings.Development.json
├── Properties/launchSettings.json # porta canônica da API (5000)
├── Controllers/                   # borda HTTP — 1 controller por recurso
├── Dtos/                          # contratos, agrupados por recurso
│   ├── Usuario/{CadastroRequest,LoginRequest,UsuarioResponse}.cs
│   ├── Ativo/
│   ├── Carteira/
│   └── Orcamento/
├── Services/                      # regras de aplicação
│   ├── UsuarioService.cs
│   ├── CarteiraService.cs
│   ├── RebalanceamentoService.cs
│   ├── CambioService.cs
│   ├── IndicadorService.cs
│   ├── RelatorioFinanceiroService.cs
│   ├── SenhaHasher.cs
│   └── Contracts/                 # interfaces injetadas (ICarteiraService, IClock, ...)
├── Models/                        # domínio + entidades persistidas
│   ├── Usuario.cs
│   ├── CarteiraSimulada.cs
│   ├── Posicao.cs
│   ├── AporteSimulado.cs
│   ├── Orcamento.cs
│   ├── AlocacaoMeta.cs
│   ├── CotacaoCambio.cs
│   └── Ativos/{Ativo,Acao,Cripto,Etf,FundoImobiliario,RendaFixa,TipoAtivo}.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── Configurations/            # 1 IEntityTypeConfiguration por entidade
│   ├── Seed/                      # seed idempotente (ADR-016)
│   └── Migrations/                # gerado por `dotnet ef`
├── Middleware/ExceptionHandlingMiddleware.cs
├── tests/Portfy.Tests/            # xUnit, excluído do build da API
└── frontend/                      # SPA
```

**Ponto de atenção obrigatório:** a raiz do repositório **é** o projeto .NET, e o SDK compila recursivamente todo `*.cs`. `Portfy.csproj` deve conter:

```xml
<PropertyGroup>
  <DefaultItemExcludes>$(DefaultItemExcludes);tests/**;frontend/**</DefaultItemExcludes>
</PropertyGroup>
```

Sem isso, `tests/**/*.cs` e `frontend/node_modules/**` entram na compilação da API.

### 8.2 Frontend

```
frontend/
├── index.html
├── package.json
├── vite.config.ts
├── tsconfig.json / tsconfig.app.json / tsconfig.node.json
├── eslint.config.js
├── .env.example                   # documenta VITE_API_BASE_URL
└── src/
    ├── main.tsx                   # bootstrap React
    ├── App.tsx                    # composição + navegação (única fonte de tela)
    ├── pages/                     # 1 pasta por tela, TSX + CSS co-localizados
    │   ├── Login/{Login.tsx,Login.css}
    │   ├── Cadastro/{Cadastro.tsx,Cadastro.css}
    │   └── Dashboard/{Dashboard.tsx,Dashboard.css}
    ├── components/                # reutilizáveis, sem conhecimento de rota
    │   └── Ativos/Ativos.tsx
    ├── services/
    │   ├── http.ts                # instância axios + interceptor de erro
    │   └── usuarioService.ts      # 1 service por recurso da API
    ├── types/usuario.ts           # espelho dos DTOs do backend
    └── styles/global.css          # reset/tokens; sem regra de tela
```

### 8.3 Migração da estrutura atual → canônica

Commits próprios, um bloco por vez, build verde antes e depois.

| De (atual) | Para (canônico) | Risco |
|---|---|---|
| `src/Login.tsx` + `Login.css` | `src/pages/Login/` | baixo |
| `src/Cadastro.tsx` | `src/pages/Cadastro/` | baixo |
| `src/Dashboard.tsx` | `src/pages/Dashboard/` | baixo |
| `src/App.css` + `src/index.css` | `src/styles/global.css` | baixo |
| `src/services/api.ts` | `src/services/http.ts` | baixo |
| interfaces dentro de `usuarioService.ts` | `src/types/usuario.ts` | baixo |
| DTOs aninhados em controllers | `Dtos/<Recurso>/` | médio (contrato) |
| estado `static` em controllers | `AppDbContext` via `Services` | **alto — P0-a** |
| `Models/Posicao.cs` comentado | apagar (classe já existe) ou extrair a classe para lá | baixo |

---

## 9. Dono único do estado

**Regra:** existe exatamente uma autoridade para dados persistidos: **SQLite via `AppDbContext`**. Nenhuma outra estrutura retém estado de domínio entre requisições.

1. `private static` de tipo de domínio em `Controllers`/`Services` é **proibido**; só `const` e caches imutáveis com ADR.
2. Singleton de domínio (`new CarteiraSimulada(10000m)` estático) **não** substitui persistência: não sobrevive a restart, não é transacional, não é isolado por usuário.
3. `lock` em objeto de domínio **não** é o mecanismo de concorrência aqui. Concorrência = `DbContext` `Scoped` (1 por requisição) + transação + controle otimista quando houver escrita concorrente real.
4. Todo `Id` é gerado pelo banco. `_proximoId++` em memória é proibido.
5. Estado de sessão do SPA (`localStorage.usuarioLogado`) **não** é autenticação — é apenas um identificador de UI (§15, item 4).

### 9.1 Modelo de dados alvo

```
Usuario(Id PK, Nome, Login UNIQUE, SenhaHash, SalarioMensal, DataRecebimento, ContaVerificada)
CarteiraSimulada(Id PK, UsuarioId FK, Nome, SaldoDisponivel)
Posicao(Id PK, CarteiraId FK, AtivoId FK, Quantidade, PrecoMedio)
AporteSimulado(Id PK, CarteiraId FK, ValorAportado, DataAporte, OrigemSalarioId)
AlocacaoMeta(Id PK, CarteiraId FK, AtivoId FK, PercentualAlvo)   -- UNIQUE(CarteiraId, AtivoId)
Orcamento(Id PK, UsuarioId FK, NomeCategoria, LimiteDefinido, PercentualDoSalario, ValorGastoAtual, EhFixo)
Ativo(Id PK, Ticker UNIQUE, Nome, Tipo, Pais, Moeda, PrecoAtual, RentabilidadeAcumulada + colunas de subtipo)
CotacaoCambio(Id PK, MoedaOrigem, MoedaDestino, Fator, AtualizadoEm)  -- UNIQUE(MoedaOrigem, MoedaDestino)
```

Herança de `Ativo`: **TPH** com discriminador `Tipo` em string (ADR-009).

### 9.2 Colaboração entre objetos (preservar — AV1 dimensão E)

Relações canônicas, já corretas no código e que **não** devem ser refeitas:

- `CarteiraSimulada` **compõe** `Posicao` (cria e remove quando a quantidade zera).
- `CarteiraSimulada` **compõe** `AporteSimulado`.
- `Posicao` **associa** `Ativo` (o ativo tem vida independente).
- `RelatorioFinanceiro` **colabora** recebendo as peças por construtor — não as cria.

Regra: composição usa navegação e ciclo de vida do dono; associação usa FK. Não inverter.

---

## 10. Contratos de API

1. Rota: `api/<recurso>` em minúsculas, plural para coleção (`api/usuarios`, `api/carteiras`). Rotas novas seguem a regra; as atuais em PascalCase (`api/Usuario`) migram em P2-f.
2. Entrada e saída **sempre** tipo nomeado em `Dtos/`. `new { ... }` e `object` proibidos.
3. JSON `camelCase`; enums como **string** (`JsonStringEnumConverter` global); datas em ISO-8601 **UTC**.
4. Status: `200` leitura/atualização com corpo · `201` + `Location` em criação · `204` exclusão · `400` payload inválido · `401` credencial inválida · `403` proibido · `404` inexistente · `409` unicidade · `422` regra de negócio violada.
5. Erros em **ProblemDetails (RFC 7807)**. O envelope `{ "mensagem": ... }` é legado e não pode ser replicado.
6. Nunca serializar entidade de domínio diretamente (evita vazar `SenhaHash`).
7. Todo DTO de resposta consumido pelo SPA tem tipo espelho em `src/types/`, com os mesmos nomes de campo. Alterar DTO do backend **exige** atualizar tipo + service + consumidor no mesmo commit.

---

## 11. Regras de domínio financeiro

Erro aqui é corrupção de dado; nenhuma destas regras é negociável.

1. **Dinheiro é `decimal` em C#** e `number` no JSON. `float`/`double` proibido. No SQLite: `HasPrecision(18, 2)`.
2. **Cálculo autoritativo é do servidor.** O frontend formata e exibe; pode pré-visualizar, nunca persistir nem decidir valor.
3. **Arredondamento monetário** em um único helper de domínio: `Math.Round(valor, 2, MidpointRounding.AwayFromZero)`. Arredondamento espalhado é proibido.
4. **Percentuais:** as alocações de uma carteira somam `100.00` com tolerância `0.01`. Resíduo é distribuído de forma **determinística**: ordenar por percentual-alvo decrescente, arredondar e atribuir o resto à primeira alocação da ordem. Nunca por sorteio ou ordem de inserção não determinística.
5. **Rebalanceamento** torna-se regra de domínio pura em `Services/RebalanceamentoService.cs`, sem I/O, com assinatura explícita: `CalcularAportes(patrimonioAtualPorAtivo, metas, valorAporte) → IReadOnlyList<AporteSugerido>`.
6. **Aleatoriedade** (simular variação de preço) não mora no domínio. Domínio expõe operação determinística (`AtualizarPreco(decimal)`); a geração do valor aleatório fica na borda via `IGeradorDeVariacao`, com semente controlável em teste.
7. **Saldo nunca negativo** sem exceção tipada: compra sem saldo é `422`, não `200` com mensagem.
8. **Câmbio** é fator explícito e persistido (`CotacaoCambio`), aplicado em uma única função. Nunca `1.0` silencioso quando a moeda é desconhecida — falha explícita (§14).

---

## 12. Persistência

Procedimento canônico (de `docs/AVISOS.txt`, alinhado a `Data/Migrations`):

```
dotnet tool install --global dotnet-ef              # uma vez por máquina
dotnet ef migrations add <Nome> -o Data/Migrations  # lê Models/ e gera o script
dotnet ef database update                           # cria/atualiza portfy.db
```

Regras:

1. **Migrations são a única forma de criar schema.** `EnsureCreated()` é proibido.
2. **A connection string tem uma única origem:** `ConnectionStrings:DefaultConnection`, lida em `Program.cs`. Hoje `Program.cs:7` lê a configuração e `Program.cs:10` **ignora** o valor, usando o literal `"Data Source=portfy.db"` — P0-c.
3. **`*.db` não vai para o git.** Adicionar `*.db`, `*.db-shm`, `*.db-wal` ao `.gitignore` da raiz — P0-d.
4. Configuração de entidade que passe de 3 linhas vai para `Data/Configurations/<Entidade>Configuration.cs`, aplicada por `ApplyConfigurationsFromAssembly`. `OnModelCreating` não acumula configuração.
5. **Não há repositório genérico.** `DbContext` já é a abstração; `IRepository<T>` é abstração especulativa proibida (§3.2). Testes usam SQLite in-memory com o mesmo `AppDbContext`.
6. Escrita que afeta mais de uma tabela usa transação única. Sem transação, sem commit parcial.
7. Seed é executado por migration e deve ser **idempotente** (ADR-016): rodar duas vezes não duplica ativo.

---

## 13. Modelos de domínio e EF Core

Conflito real: os modelos validam no construtor e expõem `private set`; o EF precisa materializar sem passar por essas validações.

1. A entidade tem **dois caminhos de criação**: construtor de domínio (valida; usado pela aplicação) e construtor protegido sem parâmetros (existe só para o EF).
2. `Id` é `public int Id { get; private set; }`. `get;`-only puro impede materialização e é proibido em entidade persistida.
3. Invariantes ficam em **métodos de domínio** (`AtualizarSalario`, `AdicionarDespesa`), não apenas no construtor: um objeto vindo do banco nunca deve poder violar invariante persistida.
4. Validação de **entrada do usuário** mora no DTO + `Services`. O modelo valida **invariante de domínio**.
5. `Usuario` não faz hash de senha; hash é de `Services/SenhaHasher.cs` (o próprio comentário em `Models/Usuario.cs:2-3` reconhece isso).
6. Nenhum modelo acessa `AppDbContext`, `IConfiguration`, `Clock`/`Random` estáticos ou serviços externos.
7. `Ativo` precisa de `Id` (hoje **não tem**) e de construtor materializável para TPH.

---

## 14. Validação e tratamento de erros

### 14.1 Três níveis, sem sobreposição

| Nível | Onde | O que valida | Como falha |
|---|---|---|---|
| Forma | `Dtos/` + `[ApiController]` | obrigatório, faixa, formato | `400` automático do ModelState |
| Negócio | `Services/` | duplicidade, saldo, soma 100%, câmbio ausente | exceção tipada → `400`/`409`/`422` |
| Invariante | `Models/` | estado impossível | `ArgumentException`/`InvalidOperationException` → `400` |

### 14.2 Política de erro

1. **Um** middleware (`Middleware/ExceptionHandlingMiddleware.cs`) traduz exceção → `ProblemDetails`. `try/catch` em action para isso é proibido.
2. Mapa canônico: `ArgumentException`/`ArgumentOutOfRangeException` → `400`; `KeyNotFoundException` → `404`; `InvalidOperationException` → `422`; `DbUpdateException` de unicidade → `409`; resto → `500` sem stack trace no corpo.
3. `catch` vazio é proibido. `catch (Exception)` só no middleware, sempre com log.
4. Mensagens em pt-BR, específicas, sem dado sensível.
5. Frontend trata **três** estados em toda chamada: `carregando`, `erro`, `vazio`; erro aparece **na tela**, nunca só em `alert`/`console`. `catch` tipado como `unknown` + narrowing; `any` proibido.
6. Interceptor em `services/http.ts` centraliza a leitura de `ProblemDetails` e a mensagem exibida (exigência da AV1, item 4).

---

## 15. Segurança

1. **CORS por allowlist** vinda de configuração. Hoje hardcoded em `Program.cs:34` (`localhost:5173`, `localhost:3000`). `AllowAnyOrigin` proibido.
2. **`AllowedHosts: "*"`** (`appsettings.json:7`) deve ser substituído por hosts explícitos por ambiente.
3. **Senhas:** PBKDF2-SHA256, salt por usuário, `CryptographicOperations.FixedTimeEquals`, ≥ 210.000 iterações (hoje 100.000). Nunca logar senha/hash/salt; nunca retornar `SenhaHash` em DTO.
4. **Autenticação não existe.** `UseAuthorization()` sem `AddAuthentication()` não protege endpoint algum (zero `[Authorize]`). É **falsa garantia de segurança**: até ADR-010, todos os endpoints são declaradamente abertos e `localStorage.usuarioLogado` não é sessão.
5. **Segredos nunca no repositório.** `appsettings.json` só valores não sensíveis; segredo real via variável de ambiente ou `dotnet user-secrets`.
6. **Login exige rate limiting** antes de produção (P1-g). Sem limite, brute-force é trivial.
7. **Chamada externa** (cotação/câmbio) exige timeout, `CancellationToken` e fallback. Falha de terceiro não derruba a API — e, no MVP, câmbio é tabela manual (ADR-015), sem chamada externa.
8. Erros `500` não expõem stack trace.

---

## 16. Convenções de código

### 16.1 C#

1. **Model nunca tem `{ get; set; }`** — só `private set`; `get;`-only puro é proibido em entidade persistida (§13, item 2). Hoje o repositório cumpre isso: preservar.
2. **Coleção interna é privada e exposta como `IReadOnlyCollection<T>`** com cópia (`AsReadOnly()`), como em `CarteiraSimulada.Posicoes`. Nunca expor `List<T>` mutável.
3. **Construtor público valida e recusa estado inválido** (`ArgumentException` com `nameof`).
4. **Polimorfismo em vez de `if`/`switch` sobre `TipoAtivo`.** Novo tipo de ativo = nova subclasse de `Ativo`, com `Tipo` e `CalcularRendimento` próprios.
5. `Nullable` habilitado: não use `!` para silenciar; trate o nulo ou falhe explicitamente.
6. I/O é `async` com sufixo `Async`, `CancellationToken` propagado, nunca `.Result`/`.Wait()`.
7. Tempo vem de `IClock`; o domínio recebe data por parâmetro. `DateTime.Now` é proibido.
8. Nomes: classe `PascalCase`; método com verbo (`AtualizarSalario`); privado `_camelCase`; `const` `PascalCase`. Uma classe pública por arquivo, com nome do arquivo igual ao da classe.
9. Tamanho: action ≤ 15 linhas, método ≤ 30, arquivo ≤ 300. Ultrapassar exige justificativa no PR.
10. Comentário explica **por quê** ou uma armadilha — nunca o "o quê". Bloco de código comentado é deletado no mesmo commit (é o caso de `Models/Posicao.cs`).

### 16.2 TypeScript / React

1. `strict` ativo; `any` proibido (gate de lint); use `unknown` + narrowing.
2. Tipos de domínio em `src/types/`; services não declaram interface duplicada.
3. Componente funcional + hooks. `class` component proibido.
4. Chamada HTTP só via `services/`; componente nunca importa `axios`.
5. CSS co-localizado por tela; estilo inline apenas para valor dinâmico calculado em runtime.
6. Textos de UI em pt-BR, com acentuação correta.

---

## 17. Receitas determinísticas

Cada receita é uma sequência de arquivos, na ordem, para execução sem invenção de caminho.

### 17.1 R1 — Restaurar o build verde (pré-requisito de tudo)

1. Em `Controllers/AtivoController.cs`, trocar o catálogo por instâncias das subclasses concretas (`Acao`, `FundoImobiliario`, `Cripto`, `Etf`, `RendaFixa`), usando os construtores reais.
2. Trocar `RentabilidadeSimulada` por `RentabilidadeAcumulada`.
3. Trocar `SimularVariacaoPreco(...)` por `AtualizarPreco(decimal)`; se a variação percentual for requisito, calcular o novo preço na borda e chamar `AtualizarPreco`.
4. `dotnet build Portfy.csproj` → **0 erros, 0 warnings**.
5. Nenhum outro arquivo muda neste passo.

**Aceite:** `dotnet build Portfy.csproj` com código de saída 0.

### 17.2 R2 — Trocar memória por SQLite (P0-a)

Ordem obrigatória, um commit por entidade:

1. `Portfy.csproj`: adicionar `DefaultItemExcludes` (§8.1) e `*.db*` ao `.gitignore` da raiz.
2. `Program.cs`: usar `UseSqlite(connectionString)` (§12, item 2) e `AddScoped` dos serviços.
3. Ajustar entidades para serem materializáveis (§13): `Id` com `private set`, construtor protegido, `Ativo.Id`, `CarteiraSimulada.Id/UsuarioId/Nome`.
4. `Data/Configurations/*` + `DbSet<>` para cada entidade.
5. `dotnet ef migrations add InitialCreate -o Data/Migrations` + `database update` em banco limpo.
6. `Services/<Recurso>Service.cs` com a regra; controller passa a injetar o service.
7. Remover `private static` dos controllers (§9, item 1).

**Aceite:** nenhum `private static` de domínio; dois `GET` idênticos após restart retornam os mesmos dados.

### 17.3 R3 — Adicionar entidade persistida (ex.: `AlocacaoMeta`)

1. `Models/AlocacaoMeta.cs` — invariantes + construtor de domínio + construtor para EF.
2. `Data/Configurations/AlocacaoMetaConfiguration.cs` — PK, FKs, `HasPrecision(18,2)`, índice único `(CarteiraId, AtivoId)`.
3. `Data/AppDbContext.cs` — `DbSet` + `ApplyConfigurationsFromAssembly`.
4. `dotnet ef migrations add AddAlocacaoMeta -o Data/Migrations` e commitar a migration.
5. `Dtos/Alocacao/*.cs`.
6. `Services/AlocacaoService.cs` — valida soma 100% (§11, item 4).
7. `Controllers/AlocacaoController.cs` — fino.
8. `frontend/src/types/alocacao.ts` + `frontend/src/services/alocacaoService.ts` + tela.
9. Gates de §18.

**Aceite:** banco limpo + `database update` cria a tabela; endpoint responde `201`; soma inválida responde `422`.

### 17.4 R4 — Adicionar endpoint

1. DTO de entrada em `Dtos/<Recurso>/` com `DataAnnotations`.
2. Método em `Services/<Recurso>Service.cs` (regra + persistência).
3. Action: `[HttpX]`, DTO nomeado, status correto, sem `try/catch`.
4. Tipo espelho em `src/types/` + método em `src/services/`.
5. Gate de §18.

### 17.5 R5 — Adicionar tela

1. `src/pages/<Tela>/<Tela>.tsx` + `<Tela>.css`.
2. Estado local; sem `axios` direto.
3. Três estados obrigatórios: carregando / erro / vazio, erro em tela (§14.2, item 5).
4. Registrar em `src/App.tsx`.
5. `npm run build && npm run lint`.

### 17.6 R6 — Fechar os critérios abertos da AV1 (F e H)

1. `Services/Contracts/ICarteiraService.cs` + implementação injetada em `Program.cs`; controller passa a depender da interface (§3.2).
2. Criar `tests/Portfy.Tests/Portfy.Tests.csproj` (xUnit) e adicionar ao `.sln`.
3. Testes negativos das invariantes existentes: compra sem saldo, venda sem posição, aporte nulo, redução maior que a quantidade, preço médio após múltiplas compras, saldo inicial negativo.
4. Testes de cálculo: rebalanceamento, soma 100%, arredondamento, câmbio sem cotação.
5. `dotnet test` verde; `dotnet build` verde.

**Aceite:** `dotnet test` com 0 falhas e ao menos 1 teste por invariante citada.

---

## 18. Gates de validação

### 18.1 Gates por tipo de mudança

| Tipo de mudança | Gate obrigatório | Comando |
|---|---|---|
| Backend (qualquer) | compila | `dotnet build Portfy.csproj` |
| Regra de domínio | + testes | `dotnet test tests/Portfy.Tests` |
| Endpoint novo/alterado | + smoke | Swagger em Development + 1 requisição real por verbo |
| Persistência | + migration | `dotnet ef migrations add` + `database update` em banco limpo |
| Contrato JSON | + espelho | tipo e service no frontend no mesmo commit |
| Frontend (qualquer) | compila + lint | `cd frontend && npm run build && npm run lint` |
| Segurança | revisão manual §15 | — |

### 18.2 Verificações estáticas (derivadas da AV1)

Todas devem retornar **vazio** (ou o valor indicado). São baratas e executáveis por qualquer agente.

| Regra | Comando | Esperado |
|---|---|---|
| Sem setter público em modelos | `grep -rn "get; set;" Models` | vazio |
| Sem estado estático de domínio | `grep -rn "private static" Controllers Services` | vazio |
| Sem `Console` em `Services` | `grep -rn "Console\." Services` | vazio |
| Sem `alert` no frontend | `grep -rn "alert(" frontend/src` | vazio |
| Sem `any` explícito | `cd frontend && npm run lint` | 0 erros |
| Sem `EnsureCreated` | `grep -rn "EnsureCreated" .` | vazio |
| Sem `DateTime.Now` | `grep -rn "DateTime.Now" --include=*.cs .` | vazio |
| Build | `dotnet build Portfy.csproj` | 0 erros |
| Testes | `dotnet test tests/Portfy.Tests` | 0 falhas |

### 18.3 Regras de gate

1. **Build ou lint vermelho bloqueia merge.** Hoje ambos estão vermelhos (§4): toda entrega começa por R1 e P1-f.
2. Falha pré-existente não pode ser reportada como corrigida pela mudança atual.
3. `--no-verify` em hooks é proibido.
4. Retry limitado a duas tentativas por classe de falha; sem mudança de estado, parar e reportar.
5. Resultado de gate é registrado (comando + saída + data) no relatório da mudança.

---

## 19. Decisões vinculantes

| ID | Decisão | Consequência |
|---|---|---|
| ADR-001 | API na raiz do repositório; testes em `tests/`; `frontend/` e `tests/` excluídos via `DefaultItemExcludes` | sem reorganização para `src/` |
| ADR-002 | SQLite/EF Core é a **única** autoridade de estado | `static` mutável e singleton de domínio proibidos |
| ADR-003 | Controller nunca toca `AppDbContext`; só `Services` | controller fino e testável por contrato |
| ADR-004 | DTOs em `Dtos/`, respostas tipadas | sem `new { }`, sem DTO aninhado em controller |
| ADR-005 | Erros em ProblemDetails via middleware único | sem `try/catch` por action; envelope legado congelado |
| ADR-006 | Modelo é entidade EF **e** dono de invariantes; ctor protegido para materialização | validação de entrada sai do modelo |
| ADR-007 | Dinheiro `decimal`→`number`; cálculo autoritativo só no servidor | frontend nunca persiste valor calculado |
| ADR-008 | Migrations versionadas; `EnsureCreated` e `*.db` fora do git | schema reproduzível a partir de clone limpo |
| ADR-009 | Herança de `Ativo` em TPH com discriminador string | colunas de subtipo nullable |
| ADR-010 | Autenticação real (JWT) é P1; até lá todos os endpoints são declaradamente abertos | `UseAuthorization` sem `AddAuthentication` não conta como proteção |
| ADR-011 | Sem router/SFC/gráfico até a feature existir | navegação em `App.tsx`; dependências não especulativas |
| ADR-012 | `Services` não escrevem em `Console`; relatório retorna DTO | `RelatorioFinanceiro` precisa virar service puro |
| ADR-013 | Abstração por **interface de serviço injetada** (`ICarteiraService`, `IClock`, `IGeradorDeVariacao`, `ISenhaHasher`), nunca `IRepository<T>` genérico | atende dimensão F da AV1 sem criar camada extra |
| ADR-014 | No MVP, `Orcamento` acumula gasto (`AdicionarDespesa`); lançamento datado `Despesa` fica adiado | sem tabela de lançamentos até haver relatório por período |
| ADR-015 | Câmbio do MVP é tabela `CotacaoCambio` com atualização manual; sem API externa | requer valor explícito; ausência falha (`422`), não assume 1.0 |
| ADR-016 | Seed de ativos roda em migration idempotente | MVP funciona offline com catálogo previsível |
| ADR-017 | Preview "e se?" não persiste nada | endpoint de simulação não escreve no banco |

---

## 20. Dívida técnica priorizada

### P0 — bloqueia progresso confiável

| # | Dívida | Evidência | Critério de aceite |
|---|---|---|---|
| P0-a | Três autoridades de estado (memória + memória + SQLite morto) | §4.2 | todo estado via `AppDbContext`; zero `static` de domínio (R2) |
| P0-b | Build do backend falha | §4.1 | `dotnet build` = 0 erros (R1) |
| P0-c | Connection string configurada é ignorada | `Program.cs:7` vs `:10` | `UseSqlite(connectionString)` |
| P0-d | `*.db` não ignorados no git | `.gitignore` raiz | `*.db*` ignorado |
| P0-e | Nenhuma migration versionada | `ls Data/` | `Data/Migrations/` commitado |

### P1 — necessário para o MVP ser usável com segurança

| # | Dívida | Critério de aceite |
|---|---|---|
| P1-a | Sem autenticação; login não emite credencial | JWT emitido/validado; carteira com `[Authorize]` |
| P1-b | Zero testes (dimensão H = 0,0) | `tests/Portfy.Tests` cobrindo as invariantes de R6 |
| P1-c | Erros em formato legado e duplicado | middleware único + ProblemDetails |
| P1-d | PBKDF2 com 100k iterações | ≥ 210k, parametrizado |
| P1-e | `AllowedHosts: "*"` e CORS hardcoded | allowlist por configuração |
| P1-f | `alert`/`console.error` e `any` (3 erros de lint) | erro em tela + interceptor; `npm run lint` = 0 erros |
| P1-g | Sem rate limiting no login | limite por IP/login com resposta `429` |

### P2 — qualidade e evolução

| # | Dívida | Critério de aceite |
|---|---|---|
| P2-a | `Services/RelatorioFinanceiro.cs` escreve em console | retorna DTO (ADR-012) |
| P2-b | `Models/Posicao.cs` inteiro comentado | arquivo removido ou classe extraída |
| P2-c | Namespace `Portfy.Models` em `Models/Ativos/` | `Portfy.Models.Ativos` em commit isolado |
| P2-d | `DateTime.Now` em modelos | `IClock` + `DateTime.UtcNow` na borda |
| P2-e | Sem `.env.example` / `VITE_API_BASE_URL` | base URL por ambiente, sem literal |
| P2-f | Rotas PascalCase (`api/Usuario`) | rotas em minúsculas/plural |
| P2-g | Comentários didáticos extensos nos controllers | comentário só para "por quê"/armadilha |

---

## 21. Definição de Pronto (DoD)

Uma mudança só é entregue quando **todos** são verdadeiros:

1. `dotnet build` com 0 erros e sem warning novo.
2. `npm run build && npm run lint` verdes, se o frontend foi tocado.
3. Regra de negócio nova/alterada coberta por teste automatizado.
4. Contrato de API espelhado em `src/types/` e `src/services/` no mesmo commit.
5. Migration incluída, se o schema mudou.
6. Nenhuma proibição de §6.2, §7 ou §9 introduzida; nenhuma verificação de §18.2 piorada.
7. Nenhum `TODO` silencioso, `catch` vazio, código comentado ou teste desabilitado.
8. Saída dos gates registrada no relatório da mudança.

---

## 22. O que este documento não decide

Deploy/hospedagem, CI, observabilidade e métricas, design system e acessibilidade, internacionalização, API pública versionada, performance/caching. Enquanto não existirem, valem §1 e §5.3.
