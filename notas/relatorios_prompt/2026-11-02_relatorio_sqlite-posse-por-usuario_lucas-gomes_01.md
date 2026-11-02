# Relatório — SQLite com posse por usuário e carteira operável pela API

| Item | Valor |
|---|---|
| Repositório | `C:\Users\Lucas\Desktop\projetos\portfy` |
| Data | 2026-11-02 |
| Autor | Lucas Gomes |
| Prompt de origem | Solicitação direta do dono do projeto; nenhum arquivo de prompt foi usado como fonte de instrução |
| Escopo | Tornar o SQLite utilizável por usuário, com carteira e orçamento próprios, compra/venda e aporte afetando o saldo, para liberar o trabalho no frontend |
| VCS | Nenhum commit, merge, push, tag ou operação de git foi executado pelo agente |

---

## 1. Diagnóstico inicial

O SQLite já existia e os 150 testes anteriores passavam, mas o modelo ainda era global:

- Existia **uma única carteira** (`SeedInicial.CarteiraPadraoId`) e **um único orçamento** (`SeedInicial.OrcamentoPadraoId`) no seed da migration.
- Todos os usuários liam e alteravam o mesmo saldo, as mesmas posições e o mesmo gasto.
- `AporteSimulado` não tinha `CarteiraId` no domínio; a FK `CarteiraSimuladaId` era uma *shadow property* anulável.
- `AporteService.Criar` gravava o aporte, mas não chamava `CarteiraSimulada.AdicionarAporte`; portanto, um aporte pela API não alterava o saldo.
- `ComprarAtivo` e `VenderAtivo` existiam apenas no domínio; nenhum endpoint os chamava.
- O `Dashboard.tsx` exibia `R$ 0,00` fixo para saldo/patrimônio e estados vazios fixos para orçamento/aportes.

O roadmap (`docs/roadmap.md`, fases F2 e F3) já apontava exatamente essas lacunas como pré-requisito para a interface. A alteração seguiu esse desenho, sem antecipar F4 (alocação/câmbio) nem F5 (telas).

---

## 2. Ordem de execução

1. Leitura do backend, dos testes, do frontend e dos documentos de `docs/`.
2. Alteração do modelo de domínio e das configurações EF.
3. Geração e edição manual da migration `PosseDaCarteira`.
4. Alteração dos contratos, serviços, DTOs e controllers.
5. Atualização dos testes existentes e criação de testes de migração e de API.
6. Criação dos espelhos de contrato no SPA (tipos e services).
7. Validação em banco limpo, em banco antigo com dados e em banco antigo sem usuários.
8. Atualização de `docs/arch.md`, `docs/roadmap.md` e `docs/funcionalidades.md`.

---

## 3. Modelo de domínio

### 3.1 `Models/CarteiraSimulada.cs`

- Adicionados `UsuarioId` e `Nome` (ambos com `private set`; `UsuarioId` fica fora do JSON).
- O construtor de domínio passou a exigir `usuarioId > 0` e `nome` não vazio:
  - antes: `CarteiraSimulada(decimal saldoInicial)`;
  - agora: `CarteiraSimulada(int usuarioId, string nome, decimal saldoInicial)`.
- `AdicionarAporte` passou a recusar aporte de outra carteira quando a carteira já tem `Id`.
- `ComprarAtivo` passou a vincular a nova posição à carteira (`VincularCarteira`).
- `Posicao` ganhou `CarteiraId` e `AtivoId` explícitos (as colunas históricas `CarteiraSimuladaId` e `AtivoId` foram preservadas).
- O construtor de `Posicao` preenche `AtivoId` com o `Id` do ativo quando ele já está persistido.

### 3.2 `Models/AporteSimulado.cs`

- Adicionado `CarteiraId` obrigatório (`private set`).
- O construtor passou a exigir `carteiraId > 0`.
- `OrigemSalarioId` foi mantido para não quebrar o contrato existente; na criação pela API ele recebe o id do usuário autenticado.

### 3.3 `Models/Orcamento.cs`

- Adicionados `UsuarioId` e `EhFixo` (`private set`).
- O construtor passou a exigir `usuarioId > 0`:
  - antes: `Orcamento(string nomeCategoria, decimal limite, decimal percentual)`;
  - agora: `Orcamento(int usuarioId, string nomeCategoria, decimal limite, decimal percentual, bool ehFixo = false)`.

### 3.4 `Data/Seed/SeedInicial.cs`

- Removido o seed global de `Carteiras` e `Orcamentos`.
- Removidas as constantes `CarteiraPadraoId` e `OrcamentoPadraoId`.
- Mantidas as constantes de valores iniciais:
  - `SaldoInicialCarteira = 10000m`;
  - `NomeCarteiraPadrao = "Principal"`;
  - `CategoriaPadrao = "Alimentação"`;
  - `LimitePadrao = 1200m`;
  - `PercentualPadrao = 24m`.
- O seed da migration continua apenas com os 4 ativos (`PETR4`, `VALE3`, `HGLG11`, `BTC`).

---

## 4. Configurações EF

### 4.1 `CarteiraSimuladaConfiguration`

- `UsuarioId` obrigatório.
- `Nome` obrigatório, máximo 100.
- Índice único `(UsuarioId, Nome)`.
- FK `UsuarioId -> Usuarios.Id` com `Cascade`.
- Relação com `Posicoes` usando `HasForeignKey(p => p.CarteiraId)` e cascade.
- Relação com `Aportes` usando `HasForeignKey(a => a.CarteiraId)` e `Restrict`.

### 4.2 `AporteSimuladoConfiguration`

- `CarteiraId` obrigatório, mapeado para a coluna histórica `CarteiraSimuladaId`.
- Mantida a FK `OrigemSalarioId -> Usuarios.Id` com `Restrict`.

### 4.3 `PosicaoConfiguration`

- `CarteiraId` obrigatório, mapeado para `CarteiraSimuladaId`.
- `AtivoId` obrigatório.
- Índice único `(CarteiraId, AtivoId)`.
- FK de `Ativo` com `Restrict`.

### 4.4 `OrcamentoConfiguration`

- `UsuarioId` obrigatório.
- `EhFixo` obrigatório.
- Índice único `(UsuarioId, NomeCategoria)`.
- FK `UsuarioId -> Usuarios.Id` com `Cascade`.

---

## 5. Migration `PosseDaCarteira`

**Arquivo:** `Data/Migrations/20261102210612_PosseDaCarteira.cs`  
**Designer/snapshot:** `20261102210612_PosseDaCarteira.Designer.cs` e `AppDbContextModelSnapshot.cs` (gerados pelo EF).

A migration foi gerada pelo `dotnet-ef` e o `Up` foi reescrito à mão para não perder dados existentes:

1. Adiciona `Orcamentos.EhFixo` (`false` por padrão), `Orcamentos.UsuarioId` anulável, `Carteiras.Nome` anulável e `Carteiras.UsuarioId` anulável.
2. Remove o índice antigo `IX_Posicoes_CarteiraSimuladaId`.
3. Preenche `Carteiras.Nome`: `Id = 1` recebe `Principal`; as demais recebem `Carteira <Id>`.
4. Associa a carteira e o orçamento antigos ao primeiro usuário (`MIN(Usuarios.Id)`).
5. Cria carteira `Principal` com saldo `10000` e orçamento `Alimentação`/`1200`/`24%` para cada usuário que ainda não tenha.
6. Associa cada aporte antigo à carteira do seu `OrigemSalarioId`; se não encontrar, usa a primeira carteira.
7. Associa cada posição antiga à primeira carteira.
8. Remove órfãos:
   - aportes sem carteira ou ligados a carteira sem usuário;
   - posições sem carteira ou ligadas a carteira sem usuário;
   - carteiras e orçamentos sem usuário.
9. Torna `Carteiras.UsuarioId`, `Carteiras.Nome`, `Orcamentos.UsuarioId`, `Aportes.CarteiraSimuladaId` e `Posicoes.CarteiraSimuladaId` obrigatórios.
10. Cria os índices únicos e as FKs de posse.

O `Down` é destrutivo por definição: remove aportes, posições, carteiras e orçamentos antes de voltar ao esquema global, porque o esquema antigo não representa dono. O comportamento está documentado no próprio arquivo.

### 5.1 Validação da migration

| Cenário | Resultado |
|---|---|
| Banco limpo (InitialCreate + PosseDaCarteira) | Sobe sem erro; seed de carteira/orçamento é removido; cadastro cria os dados por usuário |
| Banco antigo com 2 usuários, 1 posição e 2 aportes | Os dados antigos foram associados ao usuário correto; os demais usuários receberam carteira/orçamento; FKs e índices únicos criados |
| Banco antigo sem usuários e com aporte órfão | O aporte e a carteira global foram removidos sem violar a FK `Restrict` |
| Aplicação das migrations no startup da API (banco limpo) | Sem erro |

Além da validação manual com SQLite/Python, foram adicionados testes de regressão em C# (`MigracaoPosseTests`).

---

## 6. Serviços e contratos

### 6.1 `ICarteiraService` / `CarteiraService`

- `ObterCarteira(int usuarioId)`.
- `Comprar(int usuarioId, string ticker, int quantidade)`.
- `Vender(int usuarioId, string ticker, int quantidade)`.
- Record `OperacaoCarteira(Carteira, Ticker, Quantidade, PrecoUnitario, ValorTotal)` para a resposta HTTP.
- Criação preguiçosa da carteira `Principal` para usuário antigo sem carteira, com tratamento de corrida pelo índice único `(UsuarioId, Nome)`.
- Compra e venda carregam a carteira com posições/ativos, consultam o ativo pelo ticker e chamam o método de domínio; o `SaveChanges` grava posição e saldo juntos.

### 6.2 `IAporteService` / `AporteService`

- `Criar(int usuarioId, decimal valorAportado, int? carteiraId = null)`.
- Record `AporteRealizado(Aporte, SaldoDisponivel, PatrimonioTotal)`.
- Usa sempre a carteira do usuário do token; um `carteiraId` explícito só é aceito se for o mesmo.
- Cria o aporte com `CarteiraId` explícito, chama `CarteiraSimulada.AdicionarAporte` e grava aporte + saldo no mesmo `SaveChanges`.

### 6.3 `IOrcamentoService` / `OrcamentoService`

- `ObterOrcamento(int usuarioId)`.
- `AdicionarDespesa(int usuarioId, decimal valor)`.
- Criação preguiçosa do orçamento padrão para usuário antigo sem orçamento, com o mesmo tratamento de corrida.

### 6.4 `UsuarioService`

- `Cadastrar` agora abre uma transação e grava, na mesma unidade:
  - usuário;
  - carteira `Principal` com saldo inicial;
  - orçamento `Alimentação`.
- `Excluir` agora abre transação e remove, nesta ordem:
  - aportes do usuário;
  - carteiras do usuário (posições saem por cascade);
  - orçamentos do usuário;
  - usuário.
- Após as exclusões em massa, o `ChangeTracker` é limpo antes de remover o usuário, evitando que o EF tente apagar duas vezes as carteiras/orçamentos já removidos (concorrência otimista).
- Mantido o seam de teste com SQLite em memória.

### 6.5 `ClaimsPortfy`

- Adicionada a extensão `ObterUsuarioId(this ClaimsPrincipal)`, que lê a claim `sub`.
- Os controllers de carteira, aporte e orçamento usam essa extensão; sem `sub`, respondem `403`.

---

## 7. Controllers e contratos HTTP

### 7.1 `CarteiraController`

- `GET /api/carteira` — carteira do usuário do token.
- `POST /api/carteira/compras` — `{ ticker, quantidade }`.
- `POST /api/carteira/vendas` — `{ ticker, quantidade }`.
- Resposta de compra/venda:
  `{ mensagem, ticker, quantidade, precoUnitario, valorTotal, saldoDisponivel, patrimonioTotal }`.
- Ticker inexistente: `404`.
- Saldo insuficiente ou posição insuficiente: `422` pelo middleware único.
- Requisição sem token: `401`; token sem `sub`: `403`.

### 7.2 `AporteController`

- `POST /api/aporte` — `{ valor, carteiraId? }`.
- Resposta:
  `{ mensagem, aporte: { id, dataAporte, valorAportado, origemSalarioId, carteiraId }, saldoDisponivel, patrimonioTotal }`.
- Sem `carteiraId`, usa a carteira `Principal` do usuário.
- `carteiraId` de outro usuário: `404`.

### 7.3 `OrcamentoController`

- `GET /api/orcamento` — orçamento do usuário do token; passou a expor `ehFixo`.
- `POST /api/orcamento/despesa` — `{ valor }`; devolve o estado atualizado do mesmo usuário.

### 7.4 DTOs alterados/criados

| Arquivo | Alteração |
|---|---|
| `Dtos/Carteira/CarteiraResponse.cs` | Adicionados `Id` e `Nome` |
| `Dtos/Carteira/OperacaoCarteiraRequest.cs` | Novo (`Ticker`, `Quantidade`) |
| `Dtos/Carteira/OperacaoCarteiraRequestValidator.cs` | Novo (ticker obrigatório, quantidade > 0) |
| `Dtos/Carteira/OperacaoCarteiraResponse.cs` | Novo |
| `Dtos/Aporte/AporteRequest.cs` | `OrigemSalarioId` substituído por `CarteiraId?` |
| `Dtos/Aporte/AporteRequestValidator.cs` | Valida `CarteiraId` apenas quando informado |
| `Dtos/Aporte/AporteResponse.cs` | Adicionado `CarteiraId` |
| `Dtos/Aporte/CriarAporteResponse.cs` | Adicionados `SaldoDisponivel` e `PatrimonioTotal` |
| `Dtos/Orcamento/OrcamentoResponse.cs` | Adicionado `EhFixo` |

---

## 8. Testes

### 8.1 Novos

| Arquivo | Casos | Cobertura |
|---|---:|---|
| `tests/Portfy.Tests/Api/CarteiraApiTests.cs` | 8 | cadastro cria carteira/orçamento, compra, venda, aporte, isolamento entre usuários, saldo insuficiente, ticker inexistente, orçamento isolado |
| `tests/Portfy.Tests/Data/MigracaoPosseTests.cs` | 2 | migration em banco antigo com dados e em banco antigo sem usuários |

### 8.2 Atualizados

| Arquivo | Alteração |
|---|---|
| `tests/Portfy.Tests/Data/PersistenciaTests.cs` | Carteira/orçamento por usuário; compra/venda; aporte somando saldo; isolamento |
| `tests/Portfy.Tests/Controllers/CarteiraControllerTests.cs` | Novo contrato do serviço, claims do token, 403, 404 e mapeamento da compra |
| `tests/Portfy.Tests/Models/CarteiraSimuladaTests.cs` | Construtor com dono/nome; novos testes de usuário/nome inválidos |
| `tests/Portfy.Tests/Models/OrcamentoTests.cs` | Construtor com `usuarioId` |
| `tests/Portfy.Tests/Dtos/AporteRequestValidatorTests.cs` | `CarteiraId` opcional/ inválido |

### 8.3 Contagem final

- Antes: 150 casos.
- Depois: **170 casos**, 0 falhas.
- Distribuição por namespace: `Models` 47, `Services` 21, `Data` 10, `Dtos` 20, `Controllers` 6, `Api` 13, `Integracoes` 48, `Middleware` 5.

---

## 9. Frontend

Nenhuma tela foi alterada. Foram criados apenas os espelhos de contrato e os clientes HTTP, conforme a regra do repositório de manter o SPA sincronizado com mudanças de JSON:

| Arquivo | Conteúdo |
|---|---|
| `frontend/src/types/carteira.ts` | `Carteira`, `PosicaoDaCarteira`, `AporteDaCarteira`, `OperacaoCarteira`, `AtivoDaPosicao` |
| `frontend/src/types/orcamento.ts` | `Orcamento`, `DespesaAdicionada` |
| `frontend/src/services/CarteiraService.ts` | `obter`, `comprar`, `vender` |
| `frontend/src/services/AporteService.ts` | `criar` |
| `frontend/src/services/OrcamentoService.ts` | `obter`, `adicionarDespesa` |

O `Dashboard.tsx` continua com os cards fixos em `R$ 0,00` e os painéis em estado vazio. A ligação das telas é o próximo passo natural, usando esses services.

---

## 10. Documentação atualizada

| Arquivo | Alteração |
|---|---|
| `docs/arch.md` | Versão 1.1 -> 1.2; ADR-021 (posse por usuário); baseline de migrations e testes; nota em §9.1; tabela de múltiplas simulações |
| `docs/roadmap.md` | F1/E1.1–E1.3, E2.1 e E3.1–E3.3 marcados como executados; E2.2 como parcial; contagem de testes |
| `docs/funcionalidades.md` | §4, §5, §7, §9, §10, §11, §12 e §13 alinhados ao novo comportamento |

---

## 11. O que foi removido/substituído

- Seed global de `Carteiras` e `Orcamentos` (`HasData` removido).
- Constantes `SeedInicial.CarteiraPadraoId` e `SeedInicial.OrcamentoPadraoId`.
- Construtor `CarteiraSimulada(decimal saldoInicial)`.
- Construtor de `AporteSimulado` sem `carteiraId`.
- Construtor de `Orcamento` sem `usuarioId`.
- `AporteRequest.OrigemSalarioId` como campo do corpo HTTP.
- Uso de `CarteiraSimuladaId` como *shadow property* em `AporteSimulado` e `Posicao`; agora há propriedade explícita `CarteiraId` mapeada para a coluna histórica.
- Leitura de `SeedInicial.CarteiraPadraoId`/`OrcamentoPadraoId` nos serviços.
- Comportamento em que todos os usuários compartilhavam a mesma carteira e o mesmo orçamento.
- Comportamento em que o aporte era gravado sem alterar o saldo.
- Seam de teste `new CarteiraService()` sem contexto; os testes usam um `AppDbContext` real em SQLite in-memory.

---

## 12. Validação final

| Gate | Resultado |
|---|---|
| `dotnet build Portfy.csproj` | 0 erros, 0 avisos |
| `dotnet test tests/Portfy.Tests` | 170/170, 0 falhas |
| `cd frontend && npm run build` | 82 módulos, build verde |
| `cd frontend && npm run lint` | 0 problemas |
| `get; set;` em `Models/` | vazio |
| Campo estático mutável em `Controllers`/`Services` | vazio |
| `EnsureCreated` em uso | não (apenas menções em comentários) |
| IDs globais antigos (`CarteiraPadraoId`/`OrcamentoPadraoId`) | vazio no código |
| `alert(` no SPA | vazio |

### 12.1 Smoke real da API em banco limpo

Fluxo executado com a API real e SQLite:

1. Cadastro -> carteira `Principal` com `10000`.
2. Orçamento criado: `Alimentação`, limite `1200`.
3. Compra de 10 `PETR4` -> saldo `9615`, valor total `385`.
4. Aporte de `500` -> saldo `10115`.
5. Despesa de `200` -> saldo restante do orçamento `1000`.
6. Nova leitura da carteira -> 1 posição e 1 aporte persistidos.
7. Venda de 10 `PETR4` -> saldo `10500`, posição removida.
8. Segundo usuário -> carteira isolada com `10000` e nenhuma posição.
9. Exclusão da conta -> `GET /api/carteira` com o token antigo responde `404`.

---

## 13. Pendências e próximos passos

- Ligar o `Dashboard.tsx` a `carteiraService.obter()` e `orcamentoService.obter()`.
- Implementar o CRUD de múltiplas carteiras (`POST/PUT/DELETE`) e a clonagem de simulação (E2.2/E2.3).
- Alocação-meta, rebalanceamento, simulador "e se?" e câmbio (F4).
- Relatório financeiro por API e telas que consumam os novos endpoints (F5).
- Rate limit específico para compra/venda/aporte; hoje as políticas por IP cobrem login e mercado.
- Avaliar a renomeação de `OrigemSalarioId` para `UsuarioId` (decisão aberta do diagrama de classes).
- Substituir `DateTime.Now` por `IClock` (dívida P2-d).
- Concorrência de compras simultâneas: o `lock` de domínio não protege requisições; uma estratégia de concorrência otimista ou transação com escrita serializada pode ser necessária quando houver uso concorrente real.
- `Etf` e `RendaFixa` continuam sem instância no seed e sem teste de rendimento.
- O `portfy.db` não foi deixado no repositório; ele é criado no primeiro `dotnet run`.
