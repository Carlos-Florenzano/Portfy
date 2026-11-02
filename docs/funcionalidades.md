# Funcionalidades do Portfy

Inventário funcional **derivado do código-fonte**, não do que se pretende construir.

| Item | Valor |
|---|---|
| Base de referência | `main` @ `f29addc` (2026-11-01) + alterações locais de cadastro/validação (não commitadas) |
| Última verificação | `dotnet test tests/Portfy.Tests` → **89/89 aprovados**; `dotnet build Portfy.csproj` → 0 erros, 0 warnings; `cd frontend && npm run build && npm run lint` → verdes |
| Escopo | API ASP.NET Core (`Controllers/`, `Dtos/`, `Filters/`, `Middleware/`, `Services/`, `Models/`, `Data/`) + SPA React (`frontend/src/`) |
| Não coberto | `frontend/node_modules`, `bin/`, `obj/`, `docs/`, `notas/` |

## 0. Relação com os outros documentos

- `docs/arch.md` — autoridade sobre **como** construir (camadas, gates, ADRs).
- `notas/importante/sugestoes_mvp_portfy_temp_outubro_2026.md` — autoridade sobre o **escopo funcional pretendido** do MVP (papel definido em `docs/arch.md`, §2.1). Outros documentos de apoio: `notas/importante/portfy_avaliacao_av1_professor_andre.md` (critérios de qualidade OO) e `notas/etc/AVISOS.txt` (procedimento de migration).
- **Este documento** — autoridade sobre **o que o código entrega hoje**, classe por classe.

Regra: se o código e este documento divergirem, o código vence e este documento deve ser corrigido na mesma alteração. Este documento e as notas não competem: um descreve o presente, o outro o alvo.

## 1. Como ler o estado de cada funcionalidade

| Estado | Significado |
|---|---|
| **Completa** | Domínio + endpoint + tela consumindo, e a regra coberta por teste (de domínio ou de serviço). |
| **Exposta na API, sem tela** | Endpoint funciona e testado (direta ou indiretamente), mas nenhuma tela do SPA o chama. |
| **Só no domínio** | A regra existe em classe de `Models/`, mas nenhum serviço ou endpoint a aciona. |
| **Órfã** | A classe existe e compila, nenhum consumidor a referencia. |
| **Ausente** | Não existe no código. |

O estado é persistido em SQLite: `AppDbContext` é registrado como `Scoped`, os cinco serviços o recebem por construtor e cada escrita passa por `SaveChanges`; `IGeradorDeVariacao` e `ISenhaHasher` são `Singleton` (sem estado, sem banco). O critério **Completa** abaixo mede domínio + endpoint + tela + teste — não persistência, que já é única e está descrita em §9.

## 2. Visão geral: domínio → classes

| Domínio | Classes de domínio | Serviço | Controller | Tela |
|---|---|---|---|---|
| Identidade e acesso | `Usuario` | `UsuarioService` | `UsuarioController` | `Login.tsx`, `Cadastro.tsx`, `Dashboard.tsx` |
| Carteira simulada | `CarteiraSimulada`, `Posicao` | `CarteiraService` | `CarteiraController` | **nenhuma** |
| Aportes | `AporteSimulado` | `AporteService` | `AporteController` | **nenhuma** |
| Catálogo de ativos | `Ativo`, `Acao`, `FundoImobiliario`, `Cripto`, `Etf`, `RendaFixa`, `TipoAtivo` | `AtivoService` + `GeradorDeVariacaoAleatoria` | `AtivoController` | **nenhuma** (`components/Ativos.tsx` existe mas não é importado) |
| Orçamento | `Orcamento` | `OrcamentoService` | `OrcamentoController` | **nenhuma** |
| Relatório consolidado | — | `RelatorioFinanceiro` | — | **nenhuma** |
| Persistência | `AppDbContext` | 5 serviços `Scoped` | — | — |

Observação de rastreabilidade: existe `Models/Posicao.cs`, mas o arquivo está **inteiramente comentado**. A classe `Posicao` em uso é declarada no fim de `Models/CarteiraSimulada.cs` (namespace `Portfy.Models`).

Observação de rota: as rotas neste documento aparecem em minúsculas (`api/usuario`). O backend declara `[Route("api/[controller]")]`, então a forma publicada é PascalCase (`api/Usuario`), e o roteamento do ASP.NET é insensível a caixa. A padronização para minúsculas é dívida P2-f (`docs/arch.md` §20).

## 3. Identidade e acesso (`Usuario`)

Responsabilidade declarada na classe: identidade, renda mensal e autenticação da conta.

### 3.1 Cadastro de usuário — **Completa**

`POST /api/usuario/cadastro` → `UsuarioController.Cadastrar` → `UsuarioService.Cadastrar` → `new Usuario(...)`.

O que faz: cria um usuário com nome, salário mensal, login e senha; o login passa a ser único.

Regras aplicadas, em ordem:

1. Forma (`Dtos/Usuario/CadastroRequestValidator`, FluentValidation): `nome` não vazio, 3 a 200 caracteres; `login` não vazio, 3 a 200 caracteres; `senha` de 8 a 64 caracteres; `salarioMensal >= 0`. Violação → `400` em `ProblemDetails` (RFC 7807) com `errors` por campo, produzido pelo `FluentValidationFilter`.
2. Login já existente (comparação sem diferenciar maiúsculas) → `409 Conflict` em `ProblemDetails`.
3. `Usuario` valida `id >= 0`, `nome` não vazio, `login` não vazio, hash de senha não vazio e `salário >= 0` → `ArgumentException` → `400` pelo `ExceptionHandlingMiddleware` (sem `try/catch` na action).
4. A senha vira hash **BCrypt, fator de trabalho 12** em `Services/SenhaHasher.cs` (ADR-018) antes de entrar no domínio: `Usuario` guarda apenas `SenhaHash` e nunca recebe a senha em texto puro.
5. Hash PBKDF2 gravado por versões anteriores continua verificando e é substituído por BCrypt no primeiro login bem-sucedido.
6. Toda conta nasce com `ContaVerificada = false`.
7. `Id` é gerado pelo banco (`ValueGeneratedOnAdd`); `Id = 0` significa "ainda sem identidade" e é o valor usado pelo construtor de domínio.
8. Sucesso → `201 Created` com `Location` apontando para `GET /api/usuario/{id}` e corpo `UsuarioConfirmadoResponse`.

Tela: `Cadastro.tsx` (nome, e-mail como login, salário, senha e confirmação; valida senha = confirmação no cliente).

### 3.2 Login — **Completa** (sem sessão)

`POST /api/usuario/login` → `UsuarioController.Login` → `UsuarioService.Autenticar` → `ISenhaHasher.Verificar`.

O que faz: autentica por login + senha e devolve id, nome, login e situação da conta.

- Login é comparado sem diferenciar maiúsculas; a senha é comparada pelo `ISenhaHasher`: BCrypt `Verify` (tempo constante) ou, para hash legado, PBKDF2 com `FixedTimeEquals`.
- Credencial inválida → `401`. Conta existente mas não verificada → `401` com mensagem própria.
- **Não há emissão de token, cookie ou sessão.** O SPA grava `{ id, nome, login }` em `localStorage["usuarioLogado"]` (`Login.tsx`); nenhum endpoint valida esse estado. `Dashboard.tsx` trata conteúdo inválido como "sem sessão".

### 3.3 Verificação de conta — **Completa** (sem prova de posse)

`POST /api/usuario/{id}/verificar` → `Usuario.VerificarConta()` → `ContaVerificada = true`.

O que faz: libera o login de uma conta. Não existe envio de e-mail, token ou código: qualquer cliente que conheça o `id` verifica a conta. `Cadastro.tsx` chama a verificação logo após o cadastro, de modo que o fluxo da tela nunca bloqueia no estado "não verificada".

### 3.4 Consulta de usuários — **Exposta na API, sem tela**

- `GET /api/usuario` lista todos (id, nome, login, salário, data de recebimento, conta verificada).
- `GET /api/usuario/{id}` devolve um; inexistente → `404`.

Nota de segurança: a listagem não tem autenticação nem paginação e devolve o salário de todos os usuários.

### 3.5 Atualização de dados cadastrais — **Completa**

`PUT /api/usuario/{id}` → `UsuarioService.AtualizarDados` → `Usuario.AtualizarNome` + `Usuario.AtualizarSalario`.

Tela: `Dashboard.tsx`, modo "Editar Perfil" (nome + salário). Se o nome for inválido, a exceção aborta a operação e **o salário não é alterado** (comportamento coberto por teste).

### 3.6 Atualização isolada de salário — **Exposta na API, sem tela**

`PUT /api/usuario/{id}/salario` → `AtualizarSalarioRequestValidator` exige valor `> 0` → devolve salário anterior e novo. Não há cliente em `frontend/src/services/UsuarioService.ts`.

### 3.7 Alteração de senha — **Exposta na API, sem tela**

`PUT /api/usuario/{id}/senha` → `UsuarioService.AlterarSenha`: exige a senha atual correta (verificada pelo `ISenhaHasher`) e nova senha de 8 a 64 caracteres, diferente da atual; regrava o hash BCrypt. Senha atual errada → `400`.

### 3.8 Exclusão de conta — **Completa**

`DELETE /api/usuario/{id}` → `UsuarioService.Excluir` remove do banco (`Remove` + `SaveChanges`) → `404` se não existir. Tela: `Dashboard.tsx` ("Excluir Conta", com confirmação e remoção do `localStorage`).

### 3.9 Renda disponível — **Só no domínio**

`Usuario.CalcularRendaDisponivel(totalDespesas)` = `SalarioMensal − totalDespesas`. Único consumidor é `RelatorioFinanceiro` (§8), que é órfão.

## 4. Carteira simulada (`CarteiraSimulada`, `Posicao`)

Representa a carteira de investimentos de um usuário (arch §9.1), com saldo em dinheiro e posições. A classe usa um `lock` interno (`_lock`) para serializar as próprias mutações e expõe as coleções como `IReadOnlyCollection`. Esse `lock` **não** é o mecanismo de concorrência entre requisições: quem serializa escrita no banco é `DbContext` `Scoped` + transação (`docs/arch.md` §9, item 3).

### 4.1 Saldo e patrimônio — **Exposto na API, ainda sem tela**

- `CarteiraSimulada(usuarioId, nome, saldoInicial)` recusa usuário/nome inválidos e saldo negativo. O cadastro cria a carteira `Principal` com `SeedInicial.SaldoInicialCarteira = 10000m`; `CarteiraService.ObterCarteira(usuarioId)` lê do banco e cria a padrão para usuários antigos.
- `CalcularPatrimonioTotal()` = `SaldoDisponivel + Σ (quantidade × preço atual do ativo)`.
- `GET /api/carteira` devolve `id`, `nome`, `patrimonioTotal`, `saldoDisponivel`, `posicoes` e `aportes` **do usuário do token**; sem token → `401`.
- O SPA ainda não consome esse endpoint (os cards "Saldo Disponível" e "Patrimônio Total" do `Dashboard.tsx` seguem em `R$ 0,00`), mas `frontend/src/services/CarteiraService.ts` e `AporteService.ts` já espelham o contrato.

### 4.2 Aporte na carteira — **Exposto na API**

`CarteiraSimulada.AdicionarAporte(AporteSimulado)` recusa aporte nulo e aporte de outra carteira; registra o aporte na lista e **soma o valor ao saldo disponível**. `AporteService.Criar` chama esse método e grava aporte + saldo na mesma transação (`POST /api/aporte`).

### 4.3 Compra de ativo — **Exposta na API**

`ComprarAtivo(ativo, quantidade)`:

- recusa ativo nulo e quantidade `<= 0`;
- recusa compra cujo custo total exceda o saldo (`InvalidOperationException` → `422`);
- se não existe posição para o ativo, cria uma com o preço atual; se existe, recalcula o **preço médio ponderado** (`(custo atual + custo novo) / quantidade total`);
- debita `preço × quantidade` do saldo.

`POST /api/carteira/compras` recebe `{ ticker, quantidade }`, carrega o ativo do SQLite e grava a posição e o saldo. Ticker inexistente → `404`.

### 4.4 Venda de ativo — **Exposta na API**

`VenderAtivo(ativo, quantidade)`: recusa ativo nulo, quantidade `<= 0`, ausência de posição e venda maior que a quantidade detida; credita `preço × quantidade` no saldo e **remove a posição quando ela chega a zero**. `POST /api/carteira/vendas` é simétrico à compra.

### 4.5 Posição (`Posicao`) — **Persistida por carteira**

`quantidade` e `preçoMedio` positivos; `AtualizarPrecoMedioEQuantidade` pondera o preço médio; `ReduzirQuantidade` recusa reduzir mais do que existe. As FKs `CarteiraId` e `AtivoId` são explícitas; o índice único `(CarteiraId, AtivoId)` impede posição duplicada. Não há campo de data, custo total nem resultado (lucro/prejuízo) realizado.

## 5. Aportes (`AporteSimulado`)

### 5.1 Criação — **Exposta na API, ainda sem tela**

`POST /api/aporte` → `AporteController.AdicionarAporte` → `AporteService.Criar(usuarioId, valor, carteiraId?)`.

Regras: `valor > 0`, usuário do token existe e a carteira pertence a ele; `carteiraId` omitido usa a carteira `Principal` do usuário; id de outra carteira → `404`; sucesso devolve `200` com o aporte, o `saldoDisponivel` e o `patrimonioTotal` já atualizados; `Id` é gerado pelo banco; data = `DateTime.Now` quando não informada (dívida P2-d).

O aporte nasce com `CarteiraId` explícito e `OrigemSalarioId` = usuário autenticado. `CarteiraSimulada.AdicionarAporte` soma o valor ao saldo e o `SaveChanges` grava os dois na mesma transação.

### 5.2 Histórico — **Exposto na API, ainda sem tela**

`GET /api/carteira` devolve os `aportes` da carteira do usuário autenticado. O painel "Últimos Aportes" do `Dashboard.tsx` ainda é um estado vazio fixo; `frontend/src/services/AporteService.ts` já existe para a tela futura.

## 6. Catálogo de ativos e rendimento (`Ativo` e subclasses)

`Ativo` é abstrata: `Ticker` (normalizado para maiúsculas), `Nome`, `PrecoAtual`, `RentabilidadeAcumulada` e a propriedade abstrata `Tipo`. O construtor exige ticker não vazio e preço inicial `> 0`.

`Ativo.AtualizarPreco(novoPreco)` exige preço `> 0`, arredonda para 2 casas e recalcula `RentabilidadeAcumulada = (novo − anterior) / anterior × 100`, também com 2 casas.

> Precisão: apesar do nome, `RentabilidadeAcumulada` guarda **a variação do último `AtualizarPreco`**, sobrescrita a cada chamada — não o acumulado desde a compra. É esse valor que a API devolve como `rentabilidadeAcumulada`.

Cada subclasse define seu próprio `CalcularRendimento(quantidade, parâmetro)` (polimorfismo):

| Tipo (`TipoAtivo`) | Classe | Dado próprio | Fórmula de rendimento |
|---|---|---|---|
| Ação | `Acao` | `DividendYieldAnual` (DY estimado) | `preço × qtd × (DY/100) / 12` |
| FII | `FundoImobiliario` | `RendimentoMensalPorCota` | `qtd × rendimento por cota` |
| Cripto | `Cripto` | `TaxaStakingAnual` | `preço × qtd × (taxa/100) / 12` (0 se qtd `<= 0` ou taxa `<= 0`) |
| Renda Fixa | `RendaFixa` | `TaxaAnualContratada` | `preço × qtd × (taxa/100) / 12` |
| ETF | `Etf` | `IndiceReferencia`, `TaxaAdministracaoAnual` | `preço × qtd × ((variação do índice)/100 − taxa admin mensal)` |

Todos os retornos são arredondados para 2 casas. Consumidores: apenas `tests/Portfy.Tests` — nenhum serviço, endpoint ou tela calcula rendimento hoje.

### 6.1 Listagem do catálogo — **Exposta na API, sem tela**

`GET /api/ativo` e `GET /api/ativo/{ticker}` (ticker comparado sem diferenciar maiúsculas; inexistente → `404`). Devolvem ticker, nome, tipo como string, preço atual e rentabilidade.

O catálogo vem do seed aplicado pela migration (`Data/Seed/SeedInicial.cs`), lido por `DbSet<Ativo>`, com **4 ativos**: `PETR4` (Ação, DY 12%), `VALE3` (Ação, DY 8,5%), `HGLG11` (FII, R$ 1,10/cota), `BTC` (Cripto, staking 0%). Classes `Etf` e `RendaFixa` existem e têm fórmula, mas **não há instância alguma** delas no seed.

Tela: `frontend/src/components/Ativos.tsx` consome `GET /api/ativo`, porém **não é importado por nenhum outro arquivo** — o catálogo não aparece na aplicação.

### 6.2 Simulação de variação de preço — **Exposta na API, sem tela**

`POST /api/ativo/{ticker}/simular` (corpo opcional `{ percentualCustomizado }`) → `AtivoService.SimularVariacao`.

- Sem percentual: sorteia entre **−5% e +5%** via `IGeradorDeVariacao` (`GeradorDeVariacaoAleatoria`, `Random.Shared`).
- Com percentual: aplica o valor informado (aceita negativo).
- Calcula `novoPreço = round(preçoAnterior × (1 + p/100), 2)` (arredondamento para fora do zero), chama `Ativo.AtualizarPreco` e devolve `precoAnterior`, `novoPreco` e `rentabilidadeAcumulada`.
- Ticker inexistente → `404`; erro de argumento → `400`.

A variação é **mutação persistida do catálogo**: `IAtivoService` é `Scoped`, e o novo preço é gravado por `SaveChanges`, sobrevivendo ao reinício da API (o seed é idempotente e não sobrescreve preço já simulado — `PersistenciaTests`). Nenhuma tela chama esse endpoint.

## 7. Orçamento por categoria (`Orcamento`)

### 7.1 Leitura e gasto — **Exposto na API, ainda sem tela**

- `Orcamento(usuarioId, nomeCategoria, limiteDefinido, percentualDoSalario, ehFixo = false)`: usuário/nome válidos, limite `>= 0`, percentual `>= 0`; gasto inicia em 0.
- `AdicionarDespesa(valor)` exige `valor > 0` e acumula em `ValorGastoAtual`.
- `ValidarEstouroOrcamento()` = `gasto > limite`; `ObterSaldoRestante()` = `limite − gasto` (pode ser negativo).
- `GET /api/orcamento` devolve a categoria padrão **do usuário do token**: categoria, limite, gasto, percentual, saldo restante, estouro e `ehFixo`.
- `POST /api/orcamento/despesa` exige `valor > 0` e devolve o estado atualizado do mesmo usuário.

### 7.2 Limites funcionais atuais

- O cadastro cria **um orçamento por usuário**, `"Alimentação"`, limite R$ 1.200,00, 24% do salário (`SeedInicial`). Usuários antigos recebem um na primeira leitura.
- Não há endpoint para criar, listar ou remover categorias; o índice único `(UsuarioId, NomeCategoria)` já protege a futura API.
- `PercentualDoSalario` e `EhFixo` são armazenados e devolvidos, mas **não participam de nenhum cálculo** hoje.
- `Dashboard.tsx` exibe o painel "Orçamentos" como estado vazio fixo; `frontend/src/services/OrcamentoService.ts` já espelha o contrato.

## 8. Relatório financeiro consolidado (`RelatorioFinanceiro`)

Estado: **Órfã**.

Recebe `Usuario`, `CarteiraSimulada` e `List<Orcamento>` e monta, no console, um relatório com: nome e salário do usuário; saldo disponível e patrimônio total; lista de aportes com total aportado; posições com quantidade e valor atual; gastos por categoria, total de despesas e renda disponível (`Usuario.CalcularRendaDisponivel`).

Nenhum arquivo instancia a classe (`grep RelatorioFinanceiro` só encontra a própria declaração). O efeito prático é que "relatório" não é uma funcionalidade disponível: a capacidade de consolidar dados existe, mas não é acionável por API nem por tela.

## 9. Persistência (`AppDbContext`)

Estado: **Completa** — é a autoridade única de estado (`docs/arch.md` §9).

- `Data/AppDbContext.cs` declara 5 `DbSet` (`Usuarios`, `Ativos`, `CarteirasSimuladas`, `AportesSimulados`, `Orcamentos`); `OnModelCreating` aplica `ApplyConfigurationsFromAssembly` + `SeedInicial.Aplicar`.
- `Data/Configurations/` tem 7 arquivos: chave primária, FKs, `HasPrecision(18,2)`, índice único de `Ticker` com colação `NOCASE`, hierarquia de `Ativo` em TPH com discriminador string na coluna `Tipo` e os índices de posse (`UsuarioId`+`Nome` da carteira, `UsuarioId`+`NomeCategoria` do orçamento, `CarteiraId`+`AtivoId` da posição).
- `Program.cs` lê `ConnectionStrings:DefaultConnection` (ausência derruba o startup), registra `AppDbContext` como `Scoped` com `UseSqlite` + `SqlitePragmaInterceptor` (`foreign_keys`, WAL, `busy_timeout`) e aplica `Database.Migrate()` na inicialização.
- `Data/Migrations/20261101003441_InitialCreate` cria o schema e `20261102210612_PosseDaCarteira` adiciona a posse por usuário (ADR-021). O seed da migration contém apenas os 4 ativos (ADR-016); carteira `Principal` e orçamento `Alimentação` nascem no cadastro do usuário. `portfy.db` fica fora do git (`.gitignore`, linhas 12-14).
- `Data/AppDbContextEmMemoria.cs` é o seam da suíte de testes: SQLite in-memory pelo mesmo caminho de persistência.

Consequência funcional: o estado de §3–§7 **sobrevive ao reinício da API** e é isolado por usuário, coberto por `tests/Portfy.Tests/Data/PersistenciaTests.cs` e `tests/Portfy.Tests/Api/CarteiraApiTests.cs`. O que ainda não existe: histórico/auditoria de alterações, paginação de listagens e backup.

## 10. Interface web (SPA React)

| Tela | Funcionalidade | Estado |
|---|---|---|
| `App.tsx` | Troca entre login, cadastro e dashboard por `useState` (sem router) | Completa |
| `Login.tsx` | Autenticação, mensagem de sucesso herdada do cadastro, bloqueio do botão durante a requisição | Completa |
| `Cadastro.tsx` | Cadastro + verificação automática da conta + retorno ao login com aviso | Completa |
| `Dashboard.tsx` | Carrega o usuário por id, edita nome/salário, exclui a conta, saúda o usuário | Completa |
| `Dashboard.tsx` (cards) | "Salário Mensal" real; "Saldo Disponível", "Patrimônio Total" e "Total Investido" ainda fixos em `R$ 0,00` (o SQLite já expõe a carteira) | Parcial |
| `Dashboard.tsx` (painéis) | Orçamentos, Investimentos, Aportes — todos em estado vazio fixo | Ausente |
| `Dashboard.tsx` (menu) | Itens Dashboard/Salário/Investimentos/Aportes/Relatórios com `href="#"` | Ausente |
| `components/Ativos.tsx` | Lista o catálogo de ativos | Órfã (não importado) |
| `services/api.ts` | Instância axios (`http://localhost:5000/api`), interceptor que traduz `ProblemDetails`/envelope `Mensagem` para `ErroRequisicao`, mensagem padrão e mensagem de "API fora do ar" | Completa |
| `services/UsuarioService.ts` | Clientes: cadastrar, verificarConta, obterPorId, obterTodos, atualizar, excluir, login | Completa |
| `services/AtivoService.ts` | Cliente: `obterTodos` do catálogo | Órfão (só usado por `Ativos.tsx`) |
| `services/CarteiraService.ts` | Cliente de `GET /api/carteira`, compra e venda | Completa (sem tela) |
| `services/AporteService.ts` | Cliente de `POST /api/aporte` | Completa (sem tela) |
| `services/OrcamentoService.ts` | Cliente de `GET /api/orcamento` e `POST /api/orcamento/despesa` | Completa (sem tela) |

Funil de integração: **18 endpoints** existem na API (`/api/usuario` 9, `/api/ativo` 3, `/api/carteira` 3 — leitura, compra e venda —, `/api/aporte` 1, `/api/orcamento` 2). As **telas** consomem **6** (cadastro, verificar conta, obter por id, atualizar, excluir, login). `usuarioService.obterTodos` existe no cliente e nenhuma tela o chama; `ativoService.obterTodos` só é usado pelo componente órfão `Ativos.tsx`. Os clientes de carteira, aporte e orçamento existem, mas ainda não há tela consumindo.

## 11. Regras transversais do sistema

1. **Validação na construção do objeto**: `Models/` rejeita estado inválido no construtor ou no método que altera o estado; setters são privados/get-only, e mutações passam por métodos com nome de regra (`AtualizarPreco`, `AdicionarDespesa`, `ReduzirQuantidade`).
2. **Dinheiro em `decimal`**, arredondado para 2 casas nos pontos de cálculo de preço e rendimento; exposição monetária formatada em pt-BR/BRL apenas no SPA.
3. **Erros de domínio → HTTP** pelo `ExceptionHandlingMiddleware` (único ponto): `ArgumentException`/`ArgumentOutOfRangeException` → `400`; recurso inexistente → `404`; `InvalidOperationException` → `422`; conflito de unicidade de login → `409`; falha de autenticação → `401`. Respostas de erro são `ProblemDetails` (RFC 7807) com `detail`; a validação de forma do cadastro responde `ValidationProblemDetails` com `errors` por campo.
4. **Estado persistido com concorrência controlada**: os serviços são `Scoped` e recebem `AppDbContext`; concorrência é `DbContext` por requisição + transação. O `lock` de `CarteiraSimulada` serializa as mutações internas do objeto, não requisições concorrentes (`docs/arch.md` §9, item 3).
5. **CORS restrito** a `http://localhost:5173` e `http://localhost:3000`; Swagger UI só em `Development`.
6. **Autenticação JWT**: `POST /api/usuario/login` emite o token; `[Authorize]` protege os endpoints de usuário (exceto cadastro, login e verificação), carteira, aporte e orçamento. A identidade vem da claim `sub`, nunca da URL ou do corpo. A chave de assinatura vive só em configuração/ambiente (`Auth__Jwt__Chave`).

## 12. Cobertura de testes (170 casos)

Números medidos com `dotnet test tests/Portfy.Tests` em 2026-11-02, depois da migration de posse por usuário. A coluna **Casos** é o que o `dotnet test` reporta (cada `[InlineData]` conta como caso próprio).

| Área (namespace) | Casos | O que garantem |
|---|---|---|
| `Models` | 47 | invariantes de carteira, posição, orçamento, usuário e ativo; carteira com dono obrigatório |
| `Services` | 21 | catálogo/variação, autenticação, hash BCrypt, exclusão de conta |
| `Data` | 10 | persistência real em SQLite, troca de contexto, precisão de `decimal`, isolamento de carteira/orçamento por usuário, compra/venda/aporte |
| `Dtos` | 20 | validadores de cadastro, senha, aporte (carteira opcional), despesa e simulação |
| `Controllers` | 6 | resposta da carteira montada a partir do serviço injetado, 403 sem identidade, 404 de ticker, compra mapeada |
| `Api` | 13 | autenticação/JWT/rate limit e fluxo HTTP completo de carteira, compra, venda, aporte e orçamento por usuário |
| `Integracoes` | 48 | provedores de mercado, cache, disjuntor, fallback e endpoints de mercado |
| `Middleware` | 5 | mapa de exceções para `ProblemDetails` |

Total: **170 casos, 0 falhas**. Sem teste automatizado hoje: `Etf` e `RendaFixa` (rendimento), `GeradorDeVariacaoAleatoria`, `RelatorioFinanceiro` e as telas do SPA. O fluxo HTTP de usuário/carteira/orçamento tem cobertura de ponta a ponta em `Api`.

## 13. Funcionalidades ausentes (não existem no código)

Listadas para impedir que sejam tratadas como existentes:

- CRUD de múltiplas carteiras (criar, renomear, excluir) e clonagem de simulação (E2.2/E2.3 do roadmap). O modelo já tem `UsuarioId`/`Nome` e a carteira `Principal`, mas a API só expõe a leitura.
- Alocação-meta, rebalanceamento, simulador "e se?" e câmbio (F4).
- Relatório financeiro por API e telas que consumam carteira, aporte ou orçamento (F5).
- Rate limit específico para compra/venda/aporte; hoje as políticas por IP cobrem login (10/min) e mercado (60/min).

## 14. Manutenção deste documento

Atualizar na mesma alteração que: criar/remover classe de `Models/`; adicionar/remover endpoint em `Controllers/`; mudar regra de validação de domínio; adicionar ou remover tela/serviço do SPA. O estado de cada seção (§1) deve refletir o código no commit corrente, e a tabela de cabeçalho deve registrar a base de referência e os gates executados.
