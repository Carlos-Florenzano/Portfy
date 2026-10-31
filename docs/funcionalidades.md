# Funcionalidades do Portfy

Inventário funcional **derivado do código-fonte**, não do que se pretende construir.

| Item | Valor |
|---|---|
| Base de referência | `main` @ `11c5bf4` (2026-10-31) |
| Última verificação | `dotnet test tests/Portfy.Tests` → **61/61 aprovados**; `dotnet build Portfy.csproj` → 0 erros, 0 warnings |
| Escopo | API ASP.NET Core (`Controllers/`, `Services/`, `Models/`, `Data/`) + SPA React (`frontend/src/`) |
| Não coberto | `frontend/node_modules`, `bin/`, `obj/`, `docs/`, `notas/` |

## 0. Relação com os outros documentos

- `docs/arch.md` — autoridade sobre **como** construir (camadas, gates, ADRs).
- `notas/sugestoes_mvp_portfy_temp_outubro_2026.md` — autoridade sobre o **escopo funcional pretendido** do MVP (papel definido em `docs/arch.md`, §2.1).
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

Nenhuma funcionalidade do Portfy hoje tem estado **Completa** no sentido de persistência: todo estado vive em memória (`Program.cs` registra os seis serviços como `Singleton`) e é perdido ao reiniciar o processo.

## 2. Visão geral: domínio → classes

| Domínio | Classes de domínio | Serviço | Controller | Tela |
|---|---|---|---|---|
| Identidade e acesso | `Usuario` | `UsuarioService` | `UsuarioController` | `Login.tsx`, `Cadastro.tsx`, `Dashboard.tsx` |
| Carteira simulada | `CarteiraSimulada`, `Posicao` | `CarteiraService` | `CarteiraController` | **nenhuma** |
| Aportes | `AporteSimulado` | `AporteService` | `AporteController` | **nenhuma** |
| Catálogo de ativos | `Ativo`, `Acao`, `FundoImobiliario`, `Cripto`, `Etf`, `RendaFixa`, `TipoAtivo` | `AtivoService` + `GeradorDeVariacaoAleatoria` | `AtivoController` | **nenhuma** (`components/Ativos.tsx` existe mas não é importado) |
| Orçamento | `Orcamento` | `OrcamentoService` | `OrcamentoController` | **nenhuma** |
| Relatório consolidado | — | `RelatorioFinanceiro` | — | **nenhuma** |
| Persistência | `AppDbContext` | — | — | — |

Observação de rastreabilidade: existe `Models/Posicao.cs`, mas o arquivo está **inteiramente comentado**. A classe `Posicao` em uso é declarada no fim de `Models/CarteiraSimulada.cs` (namespace `Portfy.Models`).

## 3. Identidade e acesso (`Usuario`)

Responsabilidade declarada na classe: identidade, renda mensal e autenticação da conta.

### 3.1 Cadastro de usuário — **Completa** (sem persistência)

`POST /api/usuario/cadastro` → `UsuarioController.Cadastrar` → `UsuarioService.Cadastrar` → `new Usuario(...)`.

O que faz: cria um usuário com nome, salário mensal, login e senha; o login passa a ser único.

Regras aplicadas, em ordem:

1. `nome`, `login` e `senha` não podem ser vazios → `400`.
2. `salarioMensal` não pode ser negativo → `400`.
3. login já existente (comparação sem diferenciar maiúsculas) → `409 Conflict`.
4. `Usuario` valida `id > 0`, `nome` não vazio, `login` não vazio, `senha` não vazia, `salário >= 0` → `ArgumentException` → `400`.
5. Senha é convertida em hash **PBKDF2-SHA256, 100 000 iterações, salt aleatório de 16 bytes, 32 bytes de saída**, armazenada como `salt:hash` em `SenhaHash`. A senha em texto puro não é guardada em campo algum.
6. Toda conta nasce com `ContaVerificada = false`.
7. `Id` é sequencial, gerado em memória por `UsuarioService._proximoId` (inicia em 1).
8. Sucesso → `201 Created` com `Location` apontando para `GET /api/usuario/{id}`.

Tela: `Cadastro.tsx` (nome, e-mail como login, salário, senha e confirmação; valida senha = confirmação no cliente).

### 3.2 Login — **Completa** (sem sessão)

`POST /api/usuario/login` → `UsuarioController.Login` → `UsuarioService.Autenticar` → `Usuario.VerificarSenha`.

O que faz: autentica por login + senha e devolve id, nome, login e situação da conta.

- Login é comparado sem diferenciar maiúsculas; a senha é comparada com `CryptographicOperations.FixedTimeEquals` (tempo constante).
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

`PUT /api/usuario/{id}/salario` → valida `> 0` → devolve salário anterior e novo. Não há cliente em `frontend/src/services/UsuarioService.ts`.

### 3.7 Alteração de senha — **Exposta na API, sem tela**

`PUT /api/usuario/{id}/senha` → `Usuario.AlterarSenha(senhaAtual, novaSenha)`: exige a senha atual correta e nova senha não vazia; regrava o hash. Senha atual errada → `400`.

### 3.8 Exclusão de conta — **Completa**

`DELETE /api/usuario/{id}` → remove do conjunto em memória → `404` se não existir. Tela: `Dashboard.tsx` ("Excluir Conta", com confirmação e remoção do `localStorage`).

### 3.9 Renda disponível — **Só no domínio**

`Usuario.CalcularRendaDisponivel(totalDespesas)` = `SalarioMensal − totalDespesas`. Único consumidor é `RelatorioFinanceiro` (§8), que é órfão.

## 4. Carteira simulada (`CarteiraSimulada`, `Posicao`)

Representa uma carteira única de investimentos com saldo em dinheiro e posições. Toda a classe usa um `lock` interno (`_lock`) e expõe as coleções como `IReadOnlyCollection`, de modo que duas requisições simultâneas não corrompem saldo ou listas.

### 4.1 Saldo e patrimônio — **Exposta na API, sem tela**

- `CarteiraSimulada(saldoInicial)` recusa saldo negativo; `CarteiraService` cria a carteira com **R$ 10.000,00** fixos (constante `SaldoInicialPadrao`).
- `CalcularPatrimonioTotal()` = `SaldoDisponivel + Σ (quantidade × preço atual do ativo)`.
- `GET /api/carteira` devolve `patrimonioTotal`, `saldoDisponivel`, `posicoes` e `aportes`.
- Nenhuma tela consome esse endpoint: os cards "Saldo Disponível" e "Patrimônio Total" do `Dashboard.tsx` exibem **"R$ 0,00" fixo**.

### 4.2 Aporte na carteira — **Só no domínio**

`CarteiraSimulada.AdicionarAporte(AporteSimulado)` recusa aporte nulo, registra o aporte na lista e **soma o valor ao saldo disponível**. Nenhum controller chama este método (ver §5.1).

### 4.3 Compra de ativo — **Só no domínio**

`ComprarAtivo(ativo, quantidade)`:

- recusa ativo nulo e quantidade `<= 0`;
- recusa compra cujo custo total exceda o saldo (`InvalidOperationException`);
- se não existe posição para o ativo, cria uma com o preço atual; se existe, recalcula o **preço médio ponderado** (`(custo atual + custo novo) / quantidade total`);
- debita `preço × quantidade` do saldo.

Comportamento observado a preservar: a posição é localizada por **identidade de referência** (`p.Ativo == ativo`), não por `Ticker`. Duas instâncias distintas do mesmo ticker geram duas posições.

Não existe endpoint de compra: é a funcionalidade de investimento mais central do produto e hoje só é alcançável por código/teste.

### 4.4 Venda de ativo — **Só no domínio**

`VenderAtivo(ativo, quantidade)`: recusa ativo nulo, quantidade `<= 0`, ausência de posição e venda maior que a quantidade detida; credita `preço × quantidade` no saldo e **remove a posição quando ela chega a zero**.

### 4.5 Posição (`Posicao`) — **Só no domínio**

`quantidade` e `preçoMedio` positivos; `AtualizarPrecoMedioEQuantidade` pondera o preço médio; `ReduzirQuantidade` recusa reduzir mais do que existe. Não há campo de data, custo total nem resultado (lucro/prejuízo) realizado.

## 5. Aportes (`AporteSimulado`)

### 5.1 Criação — **Exposta na API, sem tela, e sem efeito na carteira**

`POST /api/aporte` → `AporteController.AdicionarAporte` → `AporteService.Criar`.

Regras: `valor > 0` e `origemSalarioId > 0`; sucesso devolve `200` com o aporte criado; `Id` sequencial em memória; data = `DateTime.Now` quando não informada.

**Limite funcional atual:** o controller cria e devolve o aporte, mas não o entrega a `CarteiraSimulada.AdicionarAporte`. Portanto, um aporte feito pela API **não altera saldo nem patrimônio** — o efeito só existe quando o aporte é adicionado diretamente à carteira (§4.2). `origemSalarioId` não é validado contra nenhum usuário existente.

### 5.2 Histórico — **Exposta na API, sem tela**

`GET /api/carteira` devolve `aportes`. Sempre vazio no fluxo HTTP, pela razão descrita em §5.1. Nenhuma tela exibe histórico: o painel "Últimos Aportes" do `Dashboard.tsx` é um estado vazio fixo.

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

O catálogo de `AtivoService` é uma lista fixa em memória com **4 ativos**: `PETR4` (Ação, DY 12%), `VALE3` (Ação, DY 8,5%), `HGLG11` (FII, R$ 1,10/cota), `BTC` (Cripto, sem staking). Classes `Etf` e `RendaFixa` existem e têm fórmula, mas **não há instância alguma** delas no catálogo.

Tela: `frontend/src/components/Ativos.tsx` consome `GET /api/ativo`, porém **não é importado por nenhum outro arquivo** — o catálogo não aparece na aplicação.

### 6.2 Simulação de variação de preço — **Exposta na API, sem tela**

`POST /api/ativo/{ticker}/simular` (corpo opcional `{ percentualCustomizado }`) → `AtivoService.SimularVariacao`.

- Sem percentual: sorteia entre **−5% e +5%** via `IGeradorDeVariacao` (`GeradorDeVariacaoAleatoria`, `Random.Shared`).
- Com percentual: aplica o valor informado (aceita negativo).
- Calcula `novoPreço = round(preçoAnterior × (1 + p/100), 2)` (arredondamento para fora do zero), chama `Ativo.AtualizarPreco` e devolve `precoAnterior`, `novoPreco` e `rentabilidadeAcumulada`.
- Ticker inexistente → `404`; erro de argumento → `400`.

A variação é **mutação de estado do catálogo**: como `IAtivoService` é `Singleton`, o preço alterado persiste entre requisições até o processo reiniciar. Nenhuma tela chama esse endpoint.

## 7. Orçamento por categoria (`Orcamento`)

### 7.1 Leitura e gasto — **Exposta na API, sem tela**

- `Orcamento(nomeCategoria, limiteDefinido, percentualDoSalario)`: nome não vazio, limite `>= 0`, percentual `>= 0`; gasto inicia em 0.
- `AdicionarDespesa(valor)` exige `valor > 0` e acumula em `ValorGastoAtual`.
- `ValidarEstouroOrcamento()` = `gasto > limite`; `ObterSaldoRestante()` = `limite − gasto` (pode ser negativo).
- `GET /api/orcamento` devolve categoria, limite, gasto, percentual, saldo restante e indicador de estouro.
- `POST /api/orcamento/despesa` exige `valor > 0` e devolve o estado atualizado.

### 7.2 Limites funcionais atuais

- Existe **um único orçamento**, fixo em `OrcamentoService`: `"Alimentação"`, limite R$ 1.200,00, 24% do salário. Não há endpoint para criar, listar ou remover categorias.
- `PercentualDoSalario` é armazenado e devolvido, mas **não participa de nenhum cálculo** (nem deriva o limite a partir do salário do usuário).
- O orçamento não é vinculado a um usuário: todos os usuários leem e alteram o mesmo gasto.
- `Dashboard.tsx` exibe o painel "Orçamentos" como estado vazio fixo; nenhum cliente HTTP de orçamento existe no SPA.

## 8. Relatório financeiro consolidado (`RelatorioFinanceiro`)

Estado: **Órfã**.

Recebe `Usuario`, `CarteiraSimulada` e `List<Orcamento>` e monta, no console, um relatório com: nome e salário do usuário; saldo disponível e patrimônio total; lista de aportes com total aportado; posições com quantidade e valor atual; gastos por categoria, total de despesas e renda disponível (`Usuario.CalcularRendaDisponivel`).

Nenhum arquivo instancia a classe (`grep RelatorioFinanceiro` só encontra a própria declaração). O efeito prático é que "relatório" não é uma funcionalidade disponível: a capacidade de consolidar dados existe, mas não é acionável por API nem por tela.

## 9. Persistência declarada (`AppDbContext`)

Estado: **Órfã**.

- `Data/AppDbContext.cs` declara `DbSet` para `Usuario`, `Ativo`, `CarteiraSimulada`, `AporteSimulado` e `Orcamento`; `OnModelCreating` está vazio, sem chave primária declarada, sem conversões e sem configuração explícita da herança de `Ativo` (o mapeamento ficaria na convenção do EF, que nenhum gate exercita).
- `Program.cs` registra o contexto com SQLite, mas **nenhum serviço o injeta**; a string de conexão é lida de `appsettings.json` em uma variável e descartada, com `"Data Source=portfy.db"` literal.
- Não existe pasta `Data/Migrations` nem arquivo `portfy.db` no repositório.

Consequência funcional: tudo o que §3–§7 faz **desaparece ao reiniciar a API**. Não há funcionalidade de histórico, auditoria ou recuperação de dados.

## 10. Interface web (SPA React)

| Tela | Funcionalidade | Estado |
|---|---|---|
| `App.tsx` | Troca entre login, cadastro e dashboard por `useState` (sem router) | Completa |
| `Login.tsx` | Autenticação, mensagem de sucesso herdada do cadastro, bloqueio do botão durante a requisição | Completa |
| `Cadastro.tsx` | Cadastro + verificação automática da conta + retorno ao login com aviso | Completa |
| `Dashboard.tsx` | Carrega o usuário por id, edita nome/salário, exclui a conta, saúda o usuário | Completa |
| `Dashboard.tsx` (cards) | "Salário Mensal" real; "Saldo Disponível", "Patrimônio Total" e "Total Investido" fixos em `R$ 0,00` | Parcial |
| `Dashboard.tsx` (painéis) | Orçamentos, Investimentos, Aportes — todos em estado vazio fixo | Ausente |
| `Dashboard.tsx` (menu) | Itens Dashboard/Salário/Investimentos/Aportes/Relatórios com `href="#"` | Ausente |
| `components/Ativos.tsx` | Lista o catálogo de ativos | Órfã (não importado) |
| `services/api.ts` | Instância axios (`http://localhost:5000/api`), interceptor que traduz `ProblemDetails`/envelope `Mensagem` para `ErroRequisicao`, mensagem padrão e mensagem de "API fora do ar" | Completa |
| `services/UsuarioService.ts` | Clientes: cadastrar, verificarConta, obterPorId, obterTodos, atualizar, excluir, login | Completa |
| `services/AtivoService.ts` | Cliente: `obterTodos` do catálogo | Órfão (só usado por `Ativos.tsx`) |

Funil de integração: **16 endpoints** existem na API (`/api/usuario` 9, `/api/ativo` 3, `/api/carteira` 1, `/api/aporte` 1, `/api/orcamento` 2). As **telas** consomem **6** (cadastro, verificar conta, obter por id, atualizar, excluir, login). `usuarioService.obterTodos` existe no cliente e nenhuma tela o chama; `ativoService.obterTodos` só é usado pelo componente órfão `Ativos.tsx`.

## 11. Regras transversais do sistema

1. **Validação na construção do objeto**: `Models/` rejeita estado inválido no construtor ou no método que altera o estado; setters são privados/get-only, e mutações passam por métodos com nome de regra (`AtualizarPreco`, `AdicionarDespesa`, `ReduzirQuantidade`).
2. **Dinheiro em `decimal`**, arredondado para 2 casas nos pontos de cálculo de preço e rendimento; exposição monetária formatada em pt-BR/BRL apenas no SPA.
3. **Erros de domínio → HTTP**: `ArgumentException`/`ArgumentOutOfRangeException` → `400`; recurso inexistente → `404`; conflito de unicidade de login → `409`; falha de autenticação → `401`. As mensagens vão no envelope `{ Mensagem }` (ou `detail`, quando o `[ApiController]` responde `ProblemDetails`).
4. **Estado em memória com proteção de concorrência**: `UsuarioService`, `CarteiraSimulada` usam `lock`; os serviços são `Singleton` em `Program.cs`.
5. **CORS restrito** a `http://localhost:5173` e `http://localhost:3000`; Swagger UI só em `Development`.
6. **Sem autenticação/autorização de transporte**: não há `AddAuthentication`, não há `[Authorize]` em nenhum controller, e `app.UseAuthorization()` opera sem esquema configurado. Qualquer cliente alcança qualquer `id`.

## 12. Cobertura de testes (61 casos, 55 métodos)

Números medidos com `dotnet test tests/Portfy.Tests` na base `11c5bf4`. A coluna abaixo conta **métodos** de teste; teorias com `[InlineData]` geram mais de um caso.

| Área | Métodos | O que garantem |
|---|---|---|
| `CarteiraSimulada` | 11 | saldo inicial, compra sem saldo, preço médio ponderado, venda sem posição, remoção ao zerar, aporte somando ao saldo, patrimônio |
| `Posicao` | 5 | construtor, redução acima do disponível |
| `Usuario` | 11 | senha não armazenada em texto puro, hash verificável, conta nasce não verificada, troca de senha, salário |
| `Orcamento` | 6 | limite, percentual, acúmulo de gasto, estouro, saldo negativo |
| `Ativo` | 7 | ticker/preço válidos, normalização de ticker, rentabilidade, polimorfismo de `Acao`/`FII`/`Cripto` |
| `AtivoService` | 5 | variação pelo percentual sorteado, percentual customizado, ticker inexistente, catálogo |
| `UsuarioService` | 7 | ids sequenciais, login duplicado sem caixa, autenticação, exclusão, atualização |
| `CarteiraController` | 3 | resposta montada a partir do serviço injetado, serviço nulo, saldo padrão |

Sem teste hoje: `Etf` e `RendaFixa` (rendimento), `AporteSimulado`, `AporteService`, `AporteController`, `OrcamentoController`, `UsuarioController`, `GeradorDeVariacaoAleatoria`, `RelatorioFinanceiro`, `AppDbContext`, e todo o SPA.

## 13. Funcionalidades ausentes (não existem no código)

Listadas para impedir que sejam tratadas como existentes:

- Persistência real (SQLite ou qualquer banco): nada é gravado em disco.
- Compra e venda de ativos por HTTP; aporte afetando a carteira por HTTP.
- Vínculo entre usuário e carteira/orçamento/aportes: todos compartilham o mesmo estado global.
- Múltiplas carteiras ou múltiplas simulações por usuário; clonagem de simulação.
- Rebalanceamento de aportes, conversão de moedas, alocação-meta por ativo.
- Busca de ativos por nome/ticker, seed de catálogo, cadastro de novos ativos por API.
- `Despesa` datada e categorias de orçamento configuráveis; classificação fixo/variável; contas recorrentes.
- Indicador de taxa de poupança.
- Relatório consolidado exposto por API ou tela.
- Autenticação por token/sessão, autorização por usuário, recuperação de senha, 2FA.
- Histórico de preços, cotação por fonte externa, câmbio automático.
- Paginação, filtros, ordenação em qualquer listagem.
- Rotas no SPA (deep link), navegação pelo menu lateral, gráficos.

## 14. Manutenção deste documento

Atualizar na mesma alteração que: criar/remover classe de `Models/`; adicionar/remover endpoint em `Controllers/`; mudar regra de validação de domínio; adicionar ou remover tela/serviço do SPA. O estado de cada seção (§1) deve refletir o código no commit corrente, e a tabela de cabeçalho deve registrar a base de referência e os gates executados.
