# Especificação verificável — Portfy (AV2)

Este documento é a **fonte única** dos três fluxos e das cinco regras de negócio
usados na avaliação. Os demais artefatos de `docs/av2/` referenciam estes
identificadores; nenhum outro documento redefine regras.

Base verificada: branch `frontend`, commit `8e069f1` + alterações de novembro/2026
(carteira com estado nomeado e endpoint de rendimento). Gates em `auditoria-final.md`.

## 1. Convenções

| Termo | Significado |
|---|---|
| **Fluxo** | caminho ponta a ponta: HTTP → controller → serviço → domínio → persistência |
| **Regra** | condição de negócio que o domínio aplica e que tem teste de sucesso **e** de violação |
| **Aceite** | comportamento observável que a implementação precisa exibir |
| **422 / 400 / 409 / 404** | status HTTP definidos no mapa único de `Middleware/ExceptionHandlingMiddleware.cs` |

## 2. Os três fluxos

### F1 — Compra de ativo na carteira

| Item | Conteúdo |
|---|---|
| Entrada | `POST api/carteira/compras` com `{ ticker, quantidade }` |
| Cadeia | `Controllers/CarteiraController.Comprar` → `Services/CarteiraService.Comprar` → `Models/CarteiraSimulada.ComprarAtivo` → `Models/Posicao` |
| Persistência | `SaldoDisponivel` e a posição (nova ou atualizada) no SQLite via `SaveChanges` |
| Sucesso | `200` com a operação (ticker, quantidade, preço unitário, valor total) |
| Falhas | `404` ticker inexistente · `422` saldo insuficiente · `422` carteira não ativa · `400` quantidade ≤ 0 · `403` sem identidade |

**Critérios de aceite**

- CA-F1.1 — Compra com saldo suficiente reduz `SaldoDisponivel` exatamente em `preço × quantidade` e cria/atualiza a posição com `PrecoMedio` ponderado.
- CA-F1.2 — Compra com saldo insuficiente responde `422` e **não** altera saldo nem posições.
- CA-F1.3 — Duas compras do mesmo ativo produzem `PrecoMedio = (q1·p1 + q2·p2) / (q1+q2)`.
- CA-F1.4 — Compra em carteira `Suspensa` ou `Encerrada` responde `422` e não grava nada.

### F2 — Venda de ativo da carteira

| Item | Conteúdo |
|---|---|
| Entrada | `POST api/carteira/vendas` com `{ ticker, quantidade }` |
| Cadeia | `Controllers/CarteiraController.Vender` → `Services/CarteiraService.Vender` → `Models/CarteiraSimulada.VenderAtivo` → `Models/Posicao.ReduzirQuantidade` |
| Persistência | `SaldoDisponivel` acrescido e posição reduzida (removida quando zera) |
| Sucesso | `200` com a operação |
| Falhas | `404` ticker inexistente · `422` quantidade acima da posição · `422` carteira não ativa · `400` quantidade ≤ 0 |

**Critérios de aceite**

- CA-F2.1 — Venda válida aumenta o saldo em `preço × quantidade` e reduz a posição.
- CA-F2.2 — Vender mais do que a posição responde `422` e preserva a quantidade anterior.
- CA-F2.3 — Vender toda a posição remove a posição da carteira e a linha do banco.
- CA-F2.4 — Venda em carteira `Suspensa` ou `Encerrada` responde `422` sem gravação.

### F3 — Orçamento por categoria (registrar despesa)

| Item | Conteúdo |
|---|---|
| Entrada | `POST api/orcamento/despesa` (categoria padrão) ou `POST api/orcamento/{id}/despesa` |
| Cadeia | `Controllers/OrcamentoController.AdicionarDespesa[NaCategoria]` → `Services/OrcamentoService.AdicionarDespesa` → `Models/Orcamento.AdicionarDespesa` |
| Persistência | `ValorGastoAtual` acumulado no orçamento do usuário |
| Sucesso | `200` com o orçamento atualizado (limite, gasto, saldo restante) |
| Falhas | `400` valor ≤ 0 · `404` orçamento inexistente · `403` sem identidade |

**Critérios de aceite**

- CA-F3.1 — Despesa positiva soma ao `ValorGastoAtual` e reduz `ObterSaldoRestante()`.
- CA-F3.2 — Despesa ≤ 0 responde `400` e não altera o acumulado.
- CA-F3.3 — `ValidarEstouroOrcamento()` fica verdadeiro quando o gasto ultrapassa o limite, sem bloquear o lançamento (o estouro é sinalizado, não proibido).
- CA-F3.4 — O orçamento de um usuário não é afetado pela despesa de outro.

## 3. As cinco regras de negócio

| # | Regra | Onde é aplicada | Exceção → HTTP | Teste de sucesso | Teste de violação |
|---|---|---|---|---|---|
| **R1** | Compra exige saldo disponível suficiente; o saldo nunca fica negativo | `Models/CarteiraSimulada.cs:116` (`ComprarAtivo`) | `InvalidOperationException` → `422` | `InvariantesDaCarteiraTests.ComprarAtivo_CalculaPrecoMedioPonderadoAposMultiplasCompras` | `InvariantesDaCarteiraTests.ComprarAtivo_RecusaCompraSemSaldoEPreservaEstado` · `CarteiraApiTests.CompraSemSaldo_Retorna422` |
| **R2** | Venda limitada à quantidade efetivamente em posse | `Models/CarteiraSimulada.cs:151` (`VenderAtivo`) | `InvalidOperationException` → `422` | `InvariantesDaCarteiraTests.VenderAtivo_RemoveAPosicaoQuandoAQuantidadeZera` | `InvariantesDaCarteiraTests.VenderAtivo_RecusaVendaMaiorQueAPosicaoEPreservaQuantidade` |
| **R3** | Preço de ativo precisa ser positivo (na criação e na atualização) | `Models/Ativos/Ativo.cs:36` e `:47` | `ArgumentException` → `400` | `AtivoTests.AtualizarPreco_RecalculaARentabilidadeAcumulada` | `AtivoTests.AtualizarPreco_RecusaPrecoNaoPositivo` · `AtivoTests.Construtor_RecusaPrecoInicialNaoPositivo` |
| **R4** | Categoria de orçamento é única por usuário | `Data/Configurations/OrcamentoConfiguration.cs:25` (índice único) + `Services/OrcamentoService` | `DbUpdateException` de unicidade → `409` | `CarteiraApiTests.Orcamento_RenomeiaCategoriaNoPut` | `CarteiraApiTests.Orcamento_RenomearParaCategoriaExistente_Retorna409` |
| **R5** | Despesa precisa ser positiva e é acumulada no orçamento | `Models/Orcamento.cs:90-95` (`AdicionarDespesa`) | `ArgumentOutOfRangeException` → `400` | `OrcamentoTests.AdicionarDespesa_AcumulaGastoEAtualizaSaldoRestante` | `OrcamentoTests.AdicionarDespesa_RecusaValorNaoPositivo` |

### Regra adicional protegida (estado)

| # | Regra | Onde | Exceção → HTTP |
|---|---|---|---|
| **R6** | Carteira só aceita movimentação no estado `Ativa`; `Encerrada` é estado final | `Models/CarteiraSimulada.cs` (`GarantirCarteiraAtiva`, `Suspender`, `Reativar`, `Encerrar`) | `InvalidOperationException` → `422` |

R1–R5 são as cinco regras da avaliação. R6 é a invariante de ciclo de vida que
sustenta os itens A05/A06 e é verificada em `EstadosDaCarteiraTests` e
`StatusDaCarteiraPersistenciaTests`.

## 4. Estados do objeto central

`Models/StatusCarteira.cs` e `Models/CarteiraSimulada.cs`:

| Estado | Significado | Transições de saída permitidas |
|---|---|---|
| `Ativa` | aceita compra, venda e aporte | `Suspender()` → `Suspensa` · `Encerrar()` → `Encerrada` |
| `Suspensa` | bloqueada para movimentação, reversível | `Reativar()` → `Ativa` · `Encerrar()` → `Encerrada` |
| `Encerrada` | estado final; nenhuma saída | — |

Transições proibidas (recusadas antes de qualquer mutação): `Encerrada → Ativa`,
`Encerrada → Suspensa`, `Ativa → Ativa` (re-suspender), `Suspensa → Suspensa`.
A identidade é preservada: mesma instância, mesmo `Id`, mesmo dono, mesmo saldo.

## 5. Fora de escopo (declarado, não implementado)

- Endpoints para suspender/reativar/encerrar uma carteira. O ciclo de vida existe no
  domínio e é persistido, mas a API não expõe transição — o produto hoje só cria a
  carteira padrão `Ativa` (ver `docs/funcionalidades.md` §13).
- Exposição de `Status` em `CarteiraResponse`. O campo é interno ao agregado.
- Consumo do endpoint de rendimento pelo SPA.
