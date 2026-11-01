# Relatório Final — Persistência SQLite no Portfy

## 1. Diagnóstico Realizado (Antes de Qualquer Alteração)

| Item | Achado |
|---|---|
| `Program.cs` | Lia `ConnectionStrings:DefaultConnection` e descartava, usando `"Data Source=portfy.db"` literal; todos os 6 serviços registrados como `AddSingleton`. |
| `Data/AppDbContext.cs` | 5 `DbSet` declarados, `OnModelCreating` vazio; nenhum serviço o injetava. |
| Estado em Memória | `UsuarioService` (`List<Usuario>` + `_proximoId`), `AtivoService` (`List<Ativo>` com 4 ativos), `CarteiraService` (`new CarteiraSimulada(10000m)`), `AporteService` (`_proximoId++`), `OrcamentoService` (`new Orcamento("Alimentação", 1200m, 24m)`). |
| `GeradorDeVariacaoAleatoria` | Sem estado, sem `IGeradorDeVariacao` persistente; nenhuma persistência criada. |
| Models | `Posicao` ativa definida em `Models/CarteiraSimulada.cs` (o arquivo `Models/Posicao.cs` está comentado/inativo); `Ativo` sem `Id`; `CarteiraSimulada`, `Posicao` e `Orcamento` sem chave primária; coleções privadas expostas via backing fields `readonly`. |
| Testes Existentes | 61 testes com status verde no baseline. Instanciação direta dos serviços (`new UsuarioService()`, `new CarteiraService()`, etc.), exigindo preservação estrita de contratos. |

---

## 2. Arquivos Modificados e Criados

### Configuração e Composição
- `Program.cs`: leitura mandatória da connection string a partir da configuração; configuração de `UseSqlite(...)` associada ao interceptor de PRAGMAs; ciclo de vida dos 5 serviços ajustado para `AddScoped`; `IGeradorDeVariacao` mantido como `Singleton`; execução de `Database.Migrate()` na inicialização da aplicação.
- `Data/AppDbContext.cs`: invocação de `ApplyConfigurationsFromAssembly` e definição de seeds de dados com documentação de suporte.
- `.gitignore`: inclusão dos padrões `*.db`, `*.db-shm` e `*.db-wal`.
- `appsettings.json`: inalterado; `ConnectionStrings:DefaultConnection = "Data Source=portfy.db"` centralizado como fonte única.

### Camada de Serviços
- `UsuarioService.cs`, `AtivoService.cs`, `CarteiraService.cs`, `AporteService.cs`, `OrcamentoService.cs`: migração integral das coleções em memória para consultas e persistência via `AppDbContext`.

### Modelos de Domínio
| Model | Alterações Aplicadas | Justificativa |
|---|---|---|
| `Ativo` | Adicionado `Id` (`[JsonIgnore]`, `private set`) e construtor `protected`. | Identidade relacional exigida pela modelagem TPH; preservação dos contratos JSON públicos de `api/ativo` e `api/carteira`. |
| `Acao`, `FundoImobiliario`, `Cripto`, `Etf`, `RendaFixa` | Inclusão de construtor `protected`; valor padrão em `IndiceReferencia = string.Empty`. | Materialização TPH pelo Entity Framework Core contornando validações restritivas de domínio. |
| `CarteiraSimulada` | Adicionado `Id` (`[JsonIgnore]`). | Chave primária relacional para suporte a relacionamentos 1:N. |
| `Posicao` | Adicionado `Id` (`[JsonIgnore]`), construtor `protected` e inicialização de referência `Ativo = null!`. | Chave primária e preenchimento de navegação gerenciada pelo ORM. |
| `AporteSimulado` | Inclusão de construtor `protected`; ajuste de validação de `id <= 0` para `id < 0`. | Geração da chave delegada ao banco de dados (`0` representa entidade transitória). |
| `Orcamento` | Adicionado `Id` (`[JsonIgnore]`), construtor `protected` e `NomeCategoria = string.Empty`. | Chave primária relacional e suporte à materialização de entidades. |

### Camada de Controle
- `AporteController.cs`: adição de bloco `try/catch` para captura de `ArgumentException`, mapeando violações de integridade referencial para HTTP `400 Bad Request`.

### Novos Componentes Adicionados
- Mapeamentos Fluent API: `Data/Configurations/` (`UsuarioConfiguration`, `AtivoConfiguration`, `AtivosDerivadosConfigurations`, `CarteiraSimuladaConfiguration`, `PosicaoConfiguration`, `AporteSimuladoConfiguration`, `OrcamentoConfiguration`).
- Infraestrutura de Dados: `Data/Seed/SeedInicial.cs`, `Data/SqlitePragmaInterceptor.cs`, `Data/AppDbContextEmMemoria.cs`.
- Migrations: `Data/Migrations/20261101003441_InitialCreate.cs` (com artefatos associados `.Designer.cs` e `Snapshot`).
- Suíte de Testes de Integração: `tests/Portfy.Tests/Data/PersistenciaTests.cs`.

---

## 3. Decisões Arquiteturais

1. **Usuário e Identidade:** chave primária autoincrementada; restrição de unicidade em `Login` com collation `COLLATE NOCASE` para suportar validações case-insensitive; preservação do algoritmo PBKDF2 (`salt:hash`); projeção explícita em DTOs/controllers impedindo vazamento de `SenhaHash`.
2. **TPH (Table-per-Hierarchy) em Ativos:** tabela única `Ativos` discriminada pela coluna `Tipo` (`Acao`, `Fii`, `Cripto`, `Etf`, `RendaFixa`). Propriedade `Tipo` do domínio mantida como calculada e ignorada pelo mapeamento (`Ignore`).
3. **Integridade Referencial e Cascateamento:** `CarteiraSimulada` → `Posicao` com exclusão em cascata (`Cascade`); `Posicao` → `Ativo` configurado com deleção restritiva (`Restrict`); `AporteSimulado` → `Usuario` via `OrigemSalarioId` com restrição (`Restrict`).
4. **Preservação de Encapsulamento:** campos privados `_posicoes` e `_aportes` preservados como coleções `readonly`, mapeados por backing fields (`PropertyAccessMode.Field`).
5. **Precisão Monetária:** mapeamento com `HasPrecision(18, 2)` persistido como `TEXT` no driver SQLite para impedir perdas de ponto flutuante; operações aritméticas executadas no domínio sob `MidpointRounding.AwayFromZero`.
6. **PRAGMAs SQLite:** interceptor de conexões aplicando `foreign_keys=ON`, `journal_mode=WAL`, `busy_timeout=5000` e `synchronous=NORMAL`.
7. **Idempotência de Seed:** execução acoplada ao histórico de migrations via `HasData`, prevenindo recriações ou sobrescritas de alterações em tempo de execução.
8. **Isolamento de Testes Unitários:** construtores padrão dos serviços utilizando `AppDbContextEmMemoria` (SQLite in-memory dedicado por instância), preservando a suíte legada sem dependência de IO físico.
9. **Gerenciamento de Escopo:** ciclo de vida de `AppDbContext` e serviços configurado como `Scoped`, mantendo `GeradorDeVariacaoAleatoria` como `Singleton`.

---

## 4. Comandos de Banco de Dados

```bash
# Instalação global da CLI do EF Core (caso aplicável)
dotnet tool install --global dotnet-ef

# Criação da migration inicial
dotnet ef migrations add InitialCreate -o Data/Migrations

# Aplicação do esquema no arquivo físico portfy.db
dotnet ef database update