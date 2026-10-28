# ARCH — Princípios de Construção de Software do Portfy

**Versão:** 1.0
**Escopo de autoridade:** este documento é a fonte de verdade para **como** o Portfy é construído (camadas, estrutura de pastas, regras de dependência, convenções, gates). Ele não substitui `docs/funcionalidades.md` (o **quê** o produto faz) nem o `README.md` (visão geral e integrantes).
**Aplicação:** obrigatório para qualquer alteração de código, estrutura ou dependência. Toda exceção exige uma entrada em [ADR](#16-decisões-vinculantes).
**Público-alvo primário:** agentes de IA e desenvolvedores que alteram o repositório sem contexto prévio.

---

## 1. Diretriz de projeto

O Portfy será desenvolvido e evoluído majoritariamente por agentes de IA. Toda decisão de construção deve maximizar: **clareza explícita, rastreabilidade, determinismo e verificabilidade por comando**, e minimizar dependência de contexto tácito.

Consequências práticas:

1. Prefira o explícito ao elegante. Um arquivo a mais é preferível a uma convenção implícita.
2. Toda regra de negócio tem **um único dono** identificável por caminho de arquivo.
3. Todo estado persistido tem **uma única autoridade**.
4. Toda afirmação de "pronto" é verificável por um comando que falha quando a afirmação é falsa.

---

## 2. Estado atual verificado (baseline)

Medido em 2026-10-28, neste repositório. **Não é opinião — é o resultado dos comandos abaixo.**

| Item | Comando / evidência | Resultado |
|---|---|---|
| Build do backend | `dotnet build Portfy.csproj` | **FALHA — 9 erros**, todos em `Controllers/AtivoController.cs` |
| Build do frontend | `cd frontend && npm run build` (`tsc -b && vite build`) | **PASSA** (79 módulos, ~250 ms) |
| Lint do frontend | `cd frontend && npm run lint` | **FALHA — 3 erros** `@typescript-eslint/no-explicit-any` (`Cadastro.tsx:51`, `Dashboard.tsx:37`, `Login.tsx:41`) |
| Migrations EF | `ls Migrations` / `ls Data/` | **inexistentes** (só `Data/AppDbContext.cs`) |
| Projeto de testes | busca por `*Tests*` | **inexistente** |
| `AppDbContext` em uso | busca por `AppDbContext` em `*.cs` | **usado apenas em `Program.cs` e na sua própria declaração — nenhum controller consome o banco** |
| Autenticação | busca por `AddAuthentication` / `[Authorize]` | **inexistente** |
| Solution file | busca por `*.sln` | **inexistente** |
| Container / CI | `Dockerfile*`, workflows | **inexistentes** |

### 2.1 Erros de build (bloqueadores atuais)

Todos em `Controllers/AtivoController.cs`, que foi escrito contra uma versão antiga de `Ativo`:

| Linha(s) | Erro | Causa raiz |
|---|---|---|
| 18–21 | `CS0144` — não é possível instanciar tipo abstrato `Ativo` | `Ativo` virou abstrato (`Models/Ativos/Ativo.cs:6`) e o controller ainda faz `new Ativo(...)` |
| 41, 63, 97 | `CS1061` — `Ativo` não contém `RentabilidadeSimulada` | a propriedade hoje se chama `RentabilidadeAcumulada` |
| 84, 89 | `CS1061` — `Ativo` não contém `SimularVariacaoPreco` | o método hoje se chama `AtualizarPreco(decimal)` |

**Consequência normativa:** o backend está em estado vermelho e o lint do frontend também. Build e lint são gates independentes: `dotnet build` falha no backend e `npm run lint` falha no frontend. Nenhuma feature nova é aceitável antes de R1 (§14.1) e de P1-f.

### 2.2 Três autoridades de estado concorrentes

| Onde | Estado guardado | Durabilidade |
|---|---|---|
| `Controllers/UsuarioController.cs:9` (`private static List<Usuario>`) | usuários, senhas em hash | memória do processo |
| `Controllers/CarteiraController.cs:31` (`private static CarteiraSimulada`) | saldo, posições, aportes | memória do processo |
| `Program.cs:9` (`AddDbContext<AppDbContext>` + SQLite) | declarado, **mas não consumido** | arquivo `portfy.db` |

Isto viola a regra de dono único (§7) e é a dívida **P0-a**.

---

## 3. Stack canônica

Versões verificadas nos arquivos do repositório. **Trocar qualquer item abaixo exige ADR.**

### 3.1 Backend

| Camada | Tecnologia | Versão | Fonte |
|---|---|---|---|
| Runtime / SDK | .NET | `net10.0` / SDK 10.0.402 | `Portfy.csproj:4` |
| Framework web | ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`) | 10 | `Portfy.csproj:1` |
| ORM | EF Core + provider SQLite | 10.0.13 | `Portfy.csproj:11-12` |
| Tooling EF | `Microsoft.EntityFrameworkCore.Design` | 10.0.13 | `Portfy.csproj:7` |
| Documentação de API | Swashbuckle (Swagger UI, somente Development) | 6.5.0 | `Portfy.csproj:13` |
| Banco | SQLite (arquivo `portfy.db`) | — | `appsettings.json:8` |
| Análise estática | `Nullable` + `ImplicitUsings` habilitados | — | `Portfy.csproj:5-6` |

### 3.2 Frontend

| Camada | Tecnologia | Versão |
|---|---|---|
| UI | React + react-dom | 19.2.8 |
| Linguagem | TypeScript | ~6.0.2 |
| Build / dev server | Vite + `@vitejs/plugin-react` | 8.3.0 / 6.1.1 |
| HTTP client | axios (instância única) | 1.20.0 |
| Estilo | CSS puro, um arquivo por tela | — |
| Lint | ESLint 10 + typescript-eslint 8.69 | — |
| Roteamento | **nenhum** (navegação por `useState` em `src/App.tsx`) | — |

### 3.3 Regras de stack

1. **Proibido** adicionar dependência que não seja exigida por uma funcionalidade já descrita em `docs/funcionalidades.md`. Biblioteca de gráficos, router, state manager, autenticação: só quando a feature existir, com ADR.
2. **Proibido** `float`/`double` para dinheiro em qualquer camada (§9).
3. **Proibido** um segundo ORM, um segundo cliente HTTP ou um segundo framework CSS.
4. Toda versão fica fixada no manifesto (`Portfy.csproj`, `frontend/package.json`). Sem `latest`, sem faixa aberta nova.

---

## 4. Camadas

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

### 4.1 Responsabilidade única por pasta

| Pasta | Uma única responsabilidade | Motivo de mudança |
|---|---|---|
| `Controllers/` | traduzir HTTP ↔ chamadas de `Services`; decidir status code | muda o contrato de rota/verbo |
| `Dtos/` | descrever o formato de entrada e saída | muda o payload |
| `Services/` | orquestrar regra de negócio e persistência | muda a regra de negócio |
| `Models/` | proteger invariantes do domínio | muda a regra intrínseca da entidade |
| `Data/` | mapear modelo relacional e configurações EF | muda o schema |
| `Middleware/` | política transversal de erro | muda a política de erro |

### 4.2 Proibições explícitas (violações já observadas)

| Proibição | Onde está violado hoje |
|---|---|
| Controller acessar `AppDbContext` diretamente | não violado (também não usado — ver §7) |
| Controller devolver objeto anônimo (`new { ... }`) | `UsuarioController.cs`, `AtivoController.cs`, `CarteiraController.cs`, `AporteController.cs` |
| Controller conter DTO aninhado | `UsuarioController.cs:13-38`, `AtivoController.cs:24-27`, `AporteController.cs:26-30` |
| Controller duplicar `try/catch (ArgumentException)` | `UsuarioController.cs` (4 blocos) |
| Controller/Service guardar estado em `static` mutável | `UsuarioController.cs:9`, `CarteiraController.cs:31`, `AtivoController.cs:15`, `AporteController.cs:24` |
| `Services/` escrever em `Console` | `Services/RelatorioFinanceiro.cs` (`Console.Clear`, `Console.WriteLine`) |
| Código morto / duplicado | `Models/Posicao.cs` está **inteiro comentado**; a classe `Posicao` real vive dentro de `Models/CarteiraSimulada.cs` |
| Pasta ≠ namespace | `Models/Ativos/*.cs` declaram `namespace Portfy.Models` |
| Frontend usar `any` | `src/Cadastro.tsx:51`, `src/Dashboard.tsx:37`, `src/Login.tsx:41` (`catch (error: any)`) — reprovam no lint |
| Frontend fazer cálculo financeiro autoritativo | `src/Dashboard.tsx:52-59` recalcula `salarioMensal` localmente |

---

## 5. Regras de dependência

Uma seta só existe se estiver nesta tabela. Qualquer outra é violação.

### 5.1 Backend

| Origem ↓ / pode depender de → | `Controllers` | `Dtos` | `Services` | `Models` | `Data` |
|---|---|---|---|---|---|
| `Controllers` | — | ✅ | ✅ | ✅ leitura | ❌ |
| `Dtos` | ❌ | — | ❌ | ❌ | ❌ |
| `Services` | ❌ | ✅ | — | ✅ | ✅ |
| `Models` | ❌ | ❌ | ❌ | — | ❌ |
| `Data` | ❌ | ❌ | ❌ | ✅ | — |

Regras derivadas:

1. `Controllers` **não** referenciam `AppDbContext`, `Microsoft.EntityFrameworkCore` nem `Data Source`.
2. `Models` não referenciam EF Core, ASP.NET Core, `Console` ou `Guid.NewGuid()` como fonte de estado.
3. `Services` não retornam `IActionResult`, não conhecem status HTTP e não escrevem em `Console`.
4. `Dtos` são tipos de dados puros: sem lógica, sem dependência de domínio. Validação de forma via `DataAnnotations` é permitida.
5. Dependência cíclica entre pastas é sempre erro.

### 5.2 Frontend

| Origem ↓ / pode depender de → | `pages` | `components` | `services` | `types` |
|---|---|---|---|---|
| `pages` | — | ✅ | ✅ | ✅ |
| `components` | ❌ | ✅ | ✅ | ✅ |
| `services` | ❌ | ❌ | ✅ | ✅ |
| `types` | ❌ | ❌ | ❌ | — |

Regras derivadas:

1. `axios` é importado **exclusivamente** em `src/services/http.ts`.
2. `pages` não chamam `axios`/`api` diretamente — sempre via `services`.
3. `types` contém apenas `type`/`interface`; nenhum valor em runtime.

---

## 6. Estrutura de pastas canônica

### 6.1 Backend (raiz do repositório)

```
Portfy/
├── Portfy.csproj                  # manifesto: TFM, pacotes, exclusões de build
├── Portfy.sln                     # (criar quando existir o projeto de testes)
├── Program.cs                     # ÚNICO ponto de composição (DI, pipeline, CORS)
├── appsettings.json               # configuração não-secreta
├── appsettings.Development.json   # configuração local
├── Properties/
│   └── launchSettings.json        # porta canônica da API (5000)
├── Controllers/                   # borda HTTP — 1 controller por recurso
│   ├── UsuarioController.cs
│   ├── AtivoController.cs
│   ├── CarteiraController.cs
│   ├── AporteController.cs
│   └── OrcamentoController.cs
├── Dtos/                          # contratos, agrupados por recurso
│   ├── Usuario/
│   │   ├── CadastroRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── UsuarioResponse.cs
│   ├── Ativo/
│   ├── Carteira/
│   └── Orcamento/
├── Services/                      # regras de aplicação
│   ├── UsuarioService.cs
│   ├── CarteiraService.cs
│   ├── RebalanceamentoService.cs
│   ├── RelatorioFinanceiroService.cs
│   └── SenhaHasher.cs
├── Models/                        # domínio + entidades persistidas
│   ├── Usuario.cs
│   ├── CarteiraSimulada.cs
│   ├── AporteSimulado.cs
│   ├── Orcamento.cs
│   ├── Posicao.cs
│   └── Ativos/
│       ├── Ativo.cs
│       ├── Acao.cs
│       ├── Cripto.cs
│       ├── Etf.cs
│       ├── FundoImobiliario.cs
│       ├── RendaFixa.cs
│       └── TipoAtivo.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── Configurations/            # IEntityTypeConfiguration<T>, 1 arquivo por entidade
│   └── Migrations/                # gerado por `dotnet ef`
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── tests/
│   └── Portfy.Tests/              # projeto xUnit, excluído do build do Portfy.csproj
└── frontend/                      # SPA
```

**Ponto de atenção obrigatório:** a raiz do repositório **é** o projeto .NET. O SDK compila recursivamente todo `*.cs`. Por isso `Portfy.csproj` deve conter explicitamente:

```xml
<PropertyGroup>
  <DefaultItemExcludes>$(DefaultItemExcludes);tests/**;frontend/**</DefaultItemExcludes>
</PropertyGroup>
```

Sem essa linha, `tests/**/*.cs` e `frontend/node_modules/**` entram na compilação da API. Isto é requisito, não sugestão.

### 6.2 Frontend

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
    ├── App.tsx                    # composição + navegação (única fonte de tela atual)
    ├── pages/                     # 1 pasta por tela, componente + CSS co-localizados
    │   ├── Login/{Login.tsx,Login.css}
    │   ├── Cadastro/{Cadastro.tsx,Cadastro.css}
    │   └── Dashboard/{Dashboard.tsx,Dashboard.css}
    ├── components/                # reutilizáveis, sem conhecimento de rota
    │   └── Ativos/Ativos.tsx
    ├── services/
    │   ├── http.ts                # instância axios; único consumidor de `axios`
    │   └── usuarioService.ts      # 1 service por recurso da API
    ├── types/
    │   └── usuario.ts             # espelho dos DTOs do backend
    └── styles/
        └── global.css             # reset/tokens; sem regra de tela
```

Regras de nomenclatura de pasta:

- `pages/<Tela>/<Tela>.tsx` — PascalCase igual ao nome do componente.
- `components/<Dominio>/<Componente>.tsx`.
- `services/<recurso>Service.ts` — camelCase, sufixo `Service` (`usuarioService.ts`).
- `types/<recurso>.ts` — singular, espelhando o recurso da API.

### 6.3 Migração da estrutura atual → canônica

Alterações estruturais são feitas em commits próprios, um bloco por vez, com build verde antes e depois.

| De (atual) | Para (canônico) | Risco |
|---|---|---|
| `src/Login.tsx` + `Login.css` | `src/pages/Login/Login.tsx` + `Login.css` | baixo (ajustar imports em `App.tsx`) |
| `src/Cadastro.tsx` | `src/pages/Cadastro/` | baixo |
| `src/Dashboard.tsx` | `src/pages/Dashboard/` | baixo |
| `src/App.css` + `src/index.css` | `src/styles/global.css` | baixo |
| `src/services/api.ts` | `src/services/http.ts` | baixo (renomear import em 2 arquivos) |
| interfaces dentro de `usuarioService.ts` | `src/types/usuario.ts` | baixo |
| DTOs aninhados em controllers | `Dtos/<Recurso>/` | médio (mexe em contrato) |
| estado `static` em controllers | `Data/AppDbContext.cs` via `Services` | **alto — exige P0-b** |
| `Models/Posicao.cs` comentado | apagar o arquivo (classe já vive em `CarteiraSimulada.cs`) ou extrair a classe | baixo |

---

## 7. Dono único do estado

**Regra:** existe exatamente uma autoridade para dados persistidos: **SQLite acessado por `AppDbContext`**. Nenhuma outra estrutura pode reter estado de domínio entre requisições.

Consequências:

1. `private static` / `static readonly` de tipo de domínio em `Controllers` ou `Services` é **proibido**. Somente `const` e caches imutáveis justificados por ADR.
2. Singleton de domínio (`new CarteiraSimulada(10000m)` estático) **não** substitui persistência: ele sobrevive a requisições mas morre no restart, não é transacional e não é isolado por usuário.
3. `lock` em objeto de domínio **não** é mecanismo de concorrência aceitável aqui. A concorrência é resolvida por: escopo `Scoped` do `DbContext` (1 por requisição) + transação + `RowVersion`/controle otimista quando houver escrita concorrente real.
4. Todo `Id` é gerado pelo banco (`INTEGER PRIMARY KEY AUTOINCREMENT`). `_proximoId++` em memória é proibido.

### 7.1 Modelo de dados mínimo (alvo)

```
Usuario(Id PK, Nome, Login UNIQUE, SenhaHash, SalarioMensal, DataRecebimento, ContaVerificada)
Carteira(Id PK, UsuarioId FK, SaldoDisponivel, Nome)
Posicao(Id PK, CarteiraId FK, AtivoId FK, Quantidade, PrecoMedio)
Aporte(Id PK, CarteiraId FK, Valor, Data, OrigemSalarioId)
Orcamento(Id PK, UsuarioId FK, NomeCategoria, Limite, PercentualSalario, ValorGastoAtual)
Ativo(Id PK, Ticker UNIQUE, Nome, Tipo, PrecoAtual, RentabilidadeAcumulada + colunas por subtipo)
AlocacaoMeta(Id PK, CarteiraId FK, AtivoId FK, PercentualAlvo)
```

Herança de `Ativo`: **TPH** (table-per-hierarchy) com coluna discriminadora `Tipo`. É a opção de menor custo para um schema pequeno e mantém polimorfismo sem joins. Requisitos:

- `Ativo` precisa de `Id` (hoje **não tem**) e de um construtor que o EF possa usar.
- Propriedades das subclasses (`DividendYieldAnual`, etc.) ficam **nullable** no banco (consequência do TPH) — aceito e documentado.
- `TipoAtivo` persiste como `string` (não como ordinal), para que reordenar o enum não corrompa dados.

---

## 8. Contratos de API

### 8.1 Regras

1. Rota: `api/<recurso>` em minúsculas, plural quando o recurso é coleção (`api/usuarios`, `api/ativos`). O modelo atual `[Route("api/[controller]")]` gera `api/Usuario` (PascalCase) — divergência aceita até a migração, mas **novas rotas seguem a regra**.
2. Entrada e saída são **sempre** tipo nomeado em `Dtos/`. Proibido `new { ... }` como resposta e proibido `object`.
3. JSON em `camelCase`; enums serializados como **string** (`JsonStringEnumConverter` global em `Program.cs`); datas em ISO-8601 UTC (`DateTime.UtcNow`, nunca `DateTime.Now`).
4. Status codes:
   - `200` leitura/atualização com corpo; `201` + `Location` em criação; `204` exclusão bem-sucedida;
   - `400` payload inválido; `401` credencial inválida; `403` autorizado mas proibido;
   - `404` recurso inexistente; `409` conflito de unicidade (ex.: login duplicado); `422` regra de negócio violada com payload sintaticamente válido.
5. Erros seguem **ProblemDetails (RFC 7807)**. Envelope `{ "mensagem": ... }` é legado e não pode ser replicado.
6. Todo DTO de saída é explícito: nunca expor entidade de domínio diretamente (evita vazar `SenhaHash`).

### 8.2 Contrato espelhado no frontend

Todo DTO de resposta consumido pelo SPA tem um tipo correspondente em `src/types/`. Nome dos campos idêntico ao JSON (`camelCase`). Alterar um DTO do backend **exige** a atualização do tipo e do service no mesmo commit — contrato quebrado sem espelho é regressão.

---

## 9. Regras de domínio financeiro

Estas regras não são negociáveis; erro aqui é corrupção de dado.

1. **Dinheiro é `decimal` em C#** e `number` no JSON. `float`/`double` proibido. No SQLite mapear com precisão explícita: `HasPrecision(18, 2)`.
2. **Cálculo autoritativo é do servidor.** O frontend formata e exibe; pode calcular pré-visualização, jamais persistir nem decidir valor.
3. **Arredondamento monetário:** `Math.Round(valor, 2, MidpointRounding.AwayFromZero)` em exatamente um lugar — helper de domínio único. Arredondamento espalhado é proibido.
4. **Percentuais:** o conjunto de alocações de uma carteira soma `100.00` com tolerância `0.01`. A distribuição de resíduo é determinística: ordenar por percentual-alvo decrescente, aplicar arredondamento e **atribuir o resto à última alocação** (maior valor). Nunca distribuir resíduo por sorteio ou por ordem de inserção não determinística.
5. **Rebalanceamento** (calcular quanto comprar por ativo) é regra de domínio pura, sem I/O, testável com entrada/saída conhecidas. Vive em `Services/RebalanceamentoService.cs` e recebe `patrimonioAtual`, `metas`, `valorAporte`.
6. **Aleatoriedade** (simular variação de preço) não pode morar no domínio. O domínio expõe operação determinística (`AtualizarPreco(decimal)`); a geração de valor aleatório acontece na borda, com `Random` injetado e semente controlável em teste.
7. **Saldo nunca negativo** sem exceção tipada explícita; compra sem saldo é `422`, não `200` com mensagem.

---

## 10. Persistência

1. **Migrations são a única forma de criar schema.** `EnsureCreated()` é proibido.
   ```
   dotnet tool install --global dotnet-ef            # uma vez por máquina
   dotnet ef migrations add <Nome> -o Data/Migrations
   dotnet ef database update
   ```
   (procedimento já registrado em `docs/AVISOS.txt`; a pasta alvo agora é `Data/Migrations`, não `Data/`.)
2. **A connection string tem uma única origem:** `ConnectionStrings:DefaultConnection` em `appsettings.json`, lida em `Program.cs`. Hoje `Program.cs:7` lê a configuração e `Program.cs:10` **ignora** o valor, usando `"Data Source=portfy.db"` literal — corrigir é P0-c.
3. **`*.db` não vai para o git.** Adicionar `*.db`, `*.db-shm`, `*.db-wal` ao `.gitignore` da raiz (P0-d).
4. Toda configuração de entidade que passe de 3 linhas vai para `Data/Configurations/<Entidade>Configuration.cs`, aplicada por `ApplyConfigurationsFromAssembly`. Configuração não se acumula em `OnModelCreating`.
5. **Não há repositório genérico nem camada `IRepository`.** `DbContext` já é a abstração. Introduzir unidade de trabalho genérica é abstração especulativa proibida (§3.3.1). Testes usam SQLite in-memory apontando o mesmo `AppDbContext`.
6. Operações de escrita que afetam mais de uma tabela usam transação única. Sem transação, sem commit parcial.

---

## 11. Modelos de domínio e EF Core

Conflito real no código atual: os modelos **validam no construtor** e expõem `private set`; o EF Core precisa materializar sem passar por essas validações. Regra de resolução:

1. A entidade tem **dois caminhos de criação**:
   - **construtor de domínio** (valida e é usado pela aplicação);
   - **construtor protegido sem parâmetros** (existe apenas para o EF).
2. `Id` é `public int Id { get; private set; }` — leitura pública, escrita privada. `get;`-only puro (como `Usuario.Id` hoje) impede materialização e é proibido em entidade persistida.
3. Invariantes ficam em **métodos de domínio** (`AtualizarSalario`, `AdicionarDespesa`), não só no construtor. Um objeto vindo do banco nunca deve poder violar invariante persistida.
4. Validação de **entrada do usuário** não mora no modelo: mora no DTO (`DataAnnotations`) + `Services`. Modelo valida **invariante de domínio**.
5. `Usuario` não faz hash de senha. Hash é responsabilidade de `Services/SenhaHasher.cs`; o modelo recebe e guarda `SenhaHash`. (O próprio comentário em `Models/Usuario.cs:2-3` já reconhece isso.)
6. Nenhum modelo acessa `AppDbContext`, `IConfiguration` ou serviços externos.

---

## 12. Validação e tratamento de erros

### 12.1 Três níveis, sem sobreposição

| Nível | Onde | O que valida | Como falha |
|---|---|---|---|
| Forma | `Dtos/` + `[ApiController]` | campo obrigatório, faixa, formato | `400` automático do ModelState |
| Negócio | `Services/` | duplicidade, saldo, soma 100%, limites | exceção tipada → `400`/`409`/`422` |
| Invariante | `Models/` | estado impossível do objeto | `ArgumentException`/`InvalidOperationException` → `400` |

### 12.2 Política de erro

1. **Um** middleware de exceção (`Middleware/ExceptionHandlingMiddleware.cs`) traduz exceção → `ProblemDetails`. `try/catch` em action para isso é proibido.
2. Mapa canônico: `ArgumentException`/`ArgumentOutOfRangeException` → `400`; `KeyNotFoundException` → `404`; `InvalidOperationException` → `422`; `DbUpdateException` de unicidade → `409`; resto → `500` sem vazar stack trace.
3. `catch` vazio é proibido. `catch (Exception)` só no middleware, e sempre com log.
4. Mensagens de erro são em pt-BR, específicas e sem dado sensível.
5. O frontend trata **três** estados em toda chamada: `carregando`, `erro`, `vazio`. `catch` tipado como `unknown` + narrowing; `any` proibido.

---

## 13. Segurança

1. **CORS por allowlist explícita**, vinda de configuração. Hoje hardcoded em `Program.cs:34` (`localhost:5173`, `localhost:3000`). `AllowAnyOrigin` é proibido.
2. **`AllowedHosts: "*"`** (`appsettings.json:7`) deve ser substituído por hosts explícitos por ambiente.
3. **Senhas:** PBKDF2-SHA256, salt por usuário, `CryptographicOperations.FixedTimeEquals`. Iterações ≥ 210.000 (hoje 100.000). Nunca logar senha, hash ou salt. Nunca retornar `SenhaHash` em DTO.
4. **Autenticação não existe.** `app.UseAuthorization()` sem `AddAuthentication()` não protege endpoint algum (verificado: zero `[Authorize]`). Isso é **falsa garantia de segurança**. Até a implementação de JWT (ADR-010), este documento declara explicitamente: **nenhum endpoint é público nem privado — todos são abertos**, e o frontend não possui sessão autenticada (apenas `localStorage.usuarioLogado`).
5. **Segredos nunca no repositório.** `appsettings.json` só com valores não sensíveis; segredo real via variável de ambiente ou `dotnet user-secrets`.
6. **Login exige rate limiting** quando for para produção (P1). Ausência de limite é brute-force trivial.
7. **Nenhuma chamada a rede externa** (cotações, câmbio) sem timeout, sem `CancellationToken` e sem fallback — falha de terceiro não pode derrubar a API.
8. Erros `500` não expõem stack trace na resposta.

---

## 14. Receitas determinísticas

Cada receita é uma sequência de arquivos, na ordem. Serve para que um agente execute sem inventar caminho.

### 14.1 R1 — Restaurar o build verde (pré-requisito de tudo)

1. Em `Controllers/AtivoController.cs`, trocar o catálogo por instâncias das subclasses concretas (`Acao`, `FundoImobiliario`, `Cripto`, `Etf`, `RendaFixa`), usando os construtores reais de cada uma.
2. Trocar `RentabilidadeSimulada` por `RentabilidadeAcumulada`.
3. Trocar `SimularVariacaoPreco(...)` por `AtualizarPreco(decimal novoPreco)` no domínio; se a simulação percentual for requisito, calcular o novo preço **no controller** a partir de `PrecoAtual` e chamar `AtualizarPreco`.
4. `dotnet build Portfy.csproj` → **0 erros, 0 warnings**.
5. Nenhum outro arquivo muda neste passo.

**Critério de aceite:** `dotnet build Portfy.csproj` sai com código 0.

### 14.2 R2 — Adicionar uma entidade persistida (ex.: `AlocacaoMeta`)

Na ordem, sem pular:

1. `Models/AlocacaoMeta.cs` — invariantes + construtor de domínio + construtor protegido para EF.
2. `Data/Configurations/AlocacaoMetaConfiguration.cs` — chave, FK, precisão `18,2`, índice único `(CarteiraId, AtivoId)`.
3. `Data/AppDbContext.cs` — adicionar `DbSet<AlocacaoMeta>` e `ApplyConfigurationsFromAssembly`.
4. `dotnet ef migrations add AddAlocacaoMeta -o Data/Migrations` → commitar a migration.
5. `Dtos/Alocacao/AlocacaoResponse.cs` + `CriarAlocacaoRequest.cs`.
6. `Services/AlocacaoService.cs` — regra de soma 100% (§9.4).
7. `Controllers/AlocacaoController.cs` — fino: recebe DTO, chama service, devolve DTO.
8. `frontend/src/types/alocacao.ts` + `frontend/src/services/alocacaoService.ts`.
9. `frontend/src/pages/...` consumindo o service (se houver UI).
10. Gates de §15 para persistência + contrato.

**Critério de aceite:** `dotnet ef database update` em banco limpo cria a tabela; endpoint responde `201`; `npm run build` passa.

### 14.3 R3 — Adicionar endpoint

1. DTO de entrada em `Dtos/<Recurso>/` (com `DataAnnotations`).
2. Método no `Services/<Recurso>Service.cs` (regra + persistência).
3. Action no controller: `[HttpX]`, DTO nomeado, status code correto, sem `try/catch`.
4. Tipo espelho em `frontend/src/types/` + método em `frontend/src/services/`.
5. Gate de §15.

### 14.4 R4 — Adicionar tela

1. `frontend/src/pages/<Tela>/<Tela>.tsx` + `<Tela>.css` co-localizado.
2. Estado local apenas; nenhuma chamada `axios` direta.
3. Três estados obrigatórios: carregando / erro / vazio.
4. Registrar a tela em `src/App.tsx` (única fonte de navegação).
5. `npm run build && npm run lint`.

---

## 15. Gates de validação

O gate mínimo depende do tipo de mudança. "Menor gate relevante" é o critério; segurança e integridade nunca são dispensadas.

| Tipo de mudança | Gate obrigatório | Comando |
|---|---|---|
| Backend (qualquer) | compila | `dotnet build Portfy.csproj` |
| Regra de domínio | + testes | `dotnet test tests/Portfy.Tests` |
| Endpoint novo/alterado | + smoke | Swagger em Development + 1 requisição real por verbo |
| Persistência | + migration | `dotnet ef migrations add` + `dotnet ef database update` em DB limpo |
| Contrato JSON | + espelho | tipo e service no frontend atualizados no mesmo commit |
| Frontend (qualquer) | compila + lint | `cd frontend && npm run build && npm run lint` |
| Segurança | revisão manual §13 | — |

Regras:

1. **Build ou lint vermelho bloqueia merge.** Hoje o backend falha no build (§2.1) e o frontend falha no lint (§2): toda entrega começa por R1 e por P1-f.
2. Falha pré-existente não pode ser reportada como corrigida pela mudança atual.
3. `--no-verify` em hooks é proibido.
4. Retry limitado: duas tentativas por classe de falha; sem mudança de estado, parar e reportar.
5. Resultado de gate é registrado (comando + saída + data) no relatório da mudança.

---

## 16. Decisões vinculantes

| ID | Decisão | Consequência |
|---|---|---|
| ADR-001 | A API permanece na raiz do repositório; testes em `tests/`; `frontend/` e `tests/` excluídos do build via `DefaultItemExcludes` | sem reorganização para `src/`; uma linha no `.csproj` protege o build |
| ADR-002 | SQLite/EF Core é a **única** autoridade de estado | `static` mutável proibido; singleton de domínio proibido |
| ADR-003 | Controller nunca toca `AppDbContext`; só `Services` | controller fica fino e testável por contrato |
| ADR-004 | DTOs em `Dtos/`, respostas tipadas | sem `new { }`, sem DTO aninhado em controller |
| ADR-005 | Erros em ProblemDetails via middleware único | sem `try/catch` por action; envelope `{ mensagem }` é legado |
| ADR-006 | Modelo é entidade EF **e** dono de invariantes; ctor protegido sem parâmetros para materialização | validação de entrada sai do modelo; `Id` com `private set` |
| ADR-007 | Dinheiro `decimal`→`number`; cálculo autoritativo só no servidor | frontend nunca persiste valor calculado |
| ADR-008 | Migrations versionadas; `EnsureCreated` e `*.db` fora do git | schema reproduzível a partir de clone limpo |
| ADR-009 | Herança de `Ativo` em TPH com discriminador string | colunas de subtipo nullable; sem tabela por tipo |
| ADR-010 | Autenticação real (JWT) é P1; até lá todos os endpoints são declaradamente abertos | `UseAuthorization` sem `AddAuthentication` não conta como proteção |
| ADR-011 | Sem router/SFC/gráfico até a feature existir | navegação em `App.tsx`; dependências não especulativas |
| ADR-012 | `Services` não escrevem em `Console`; relatório retorna DTO | `RelatorioFinanceiro` precisa ser reescrito como service puro |

---

## 17. Dívida técnica priorizada

### P0 — bloqueia qualquer progresso confiável

| # | Dívida | Evidência | Critério de aceite |
|---|---|---|---|
| P0-a | Três autoridades de estado (memória + memória + SQLite não usado) | §2.2 | todo estado passa por `AppDbContext`; zero `static` de domínio |
| P0-b | Build do backend falha | §2.1 | `dotnet build` = 0 erros |
| P0-c | Connection string configurada é ignorada | `Program.cs:7` vs `:10` | `UseSqlite(connectionString)` lendo da configuração |
| P0-d | `*.db` não ignorados no git | `.gitignore` raiz | `*.db*` no `.gitignore` |
| P0-e | Nenhuma migration versionada | `ls Data/` | `Data/Migrations/` commitado + `database update` em clone limpo |

### P1 — necessário para o MVP ser usável com segurança

| # | Dívida | Critério de aceite |
|---|---|---|
| P1-a | Sem autenticação, login não emite credencial | JWT emitido e validado; endpoints de carteira com `[Authorize]` |
| P1-b | Sem projeto de testes | `tests/Portfy.Tests` com testes de rebalanceamento, soma 100%, saldo insuficiente, hash de senha |
| P1-c | Erros em formato legado e duplicado em cada action | middleware único + ProblemDetails + helpers removidos |
| P1-d | PBKDF2 com 100k iterações | ≥ 210k, parâmetro configurável |
| P1-e | `AllowedHosts: "*"` e CORS hardcoded | allowlist por configuração |
| P1-f | Frontend sem tratamento de erro padronizado e com `any` (3 erros de lint) | `unknown` + narrowing; componente de erro por tela; `npm run lint` = 0 erros |

### P2 — qualidade e evolução

| # | Dívida | Critério de aceite |
|---|---|---|
| P2-a | `Services/RelatorioFinanceiro.cs` escreve em console | retorna DTO; renderização removida do serviço |
| P2-b | `Models/Posicao.cs` inteiro comentado | arquivo removido ou classe extraída |
| P2-c | Namespace `Portfy.Models` em `Models/Ativos/` | `Portfy.Models.Ativos` (commit único, isolado) |
| P2-d | `DateTime.Now` em modelos | `DateTime.UtcNow` na borda; domínio recebe a data |
| P2-e | Sem `.env.example` / `VITE_API_BASE_URL` | base URL por ambiente, sem literal no código |
| P2-f | Rotas PascalCase (`api/Usuario`) | rotas em minúsculas/plural na migração |
| P2-g | Comentários didáticos extensos nos controllers | comentário só para "por quê"/armadilha; o "o quê" fica no nome |

---

## 18. Definição de Pronto (DoD)

Uma mudança só é entregue quando **todos** os itens são verdadeiros:

1. Build do backend com 0 erros e sem warning novo.
2. Build + lint do frontend passando (se o frontend foi tocado).
3. Regra de negócio nova ou alterada coberta por teste automatizado.
4. Contrato de API espelhado em `src/types/` e `src/services/` no mesmo commit.
5. Migration incluída, se o schema mudou.
6. Nenhuma das proibições de §4.2, §5 ou §7 introduzida.
7. Nenhum `TODO` silencioso, nenhum `catch` vazio, nenhum teste desabilitado.
8. Saída dos gates registrada no relatório da mudança.

---

## 19. O que este documento não decide

Ficam fora de escopo e exigem documento próprio quando forem implementados: deploy/hospedagem, CI, observabilidade e métricas, design system e acessibilidade, internacionalização, API pública versionada, e performance/caching. Enquanto não existirem, valem os princípios gerais §1 e §3.3.
