# Matriz de rastreabilidade — fluxos, regras, implementação e testes

Cada linha liga um requisito de `requisitos.md` à implementação, ao teste que o
prova e ao artefato que o descreve. Se um teste for renomeado ou removido, esta
matriz fica errada — a manutenção é obrigatória na mesma alteração.

## 1. Fluxos → implementação → testes

| Fluxo | Entrada HTTP | Implementação | Teste de integração (API + persistência) | Teste de domínio |
|---|---|---|---|---|
| **F1** Compra | `POST api/carteira/compras` | `CarteiraController.Comprar` → `CarteiraService.Comprar` → `CarteiraSimulada.ComprarAtivo` | `CarteiraApiTests.Compra_AlteraSaldoEPosicaoNoBanco` · `CompraSemSaldo_Retorna422` · `CompraComTickerInexistente_Retorna404` | `InvariantesDaCarteiraTests.ComprarAtivo_CalculaPrecoMedioPonderadoAposMultiplasCompras` |
| **F2** Venda | `POST api/carteira/vendas` | `CarteiraController.Vender` → `CarteiraService.Vender` → `CarteiraSimulada.VenderAtivo` | `CarteiraApiTests.Venda_ZeraAPosicaoEAumentaOSaldo` · `CompraEVenda_AtualizamSaldoEPosicaoNoBanco` | `InvariantesDaCarteiraTests.VenderAtivo_RemoveAPosicaoQuandoAQuantidadeZera` |
| **F3** Despesa no orçamento | `POST api/orcamento/despesa` · `POST api/orcamento/{id}/despesa` | `OrcamentoController.AdicionarDespesa[NaCategoria]` → `OrcamentoService.AdicionarDespesa` → `Orcamento.AdicionarDespesa` | `CarteiraApiTests.Orcamento_IsoladoPorUsuario` | `OrcamentoTests.AdicionarDespesa_AcumulaGastoEAtualizaSaldoRestante` |

## 2. Regras → implementação → testes

| Regra | Implementação (arquivo:linha) | Sucesso | Violação | Documento |
|---|---|---|---|---|
| **R1** saldo suficiente | `Models/CarteiraSimulada.cs:116` | `InvariantesDaCarteiraTests.ComprarAtivo_CalculaPrecoMedioPonderadoAposMultiplasCompras` | `InvariantesDaCarteiraTests.ComprarAtivo_RecusaCompraSemSaldoEPreservaEstado` · `CarteiraApiTests.CompraSemSaldo_Retorna422` | `requisitos.md` §3 |
| **R2** venda ≤ posição | `Models/CarteiraSimulada.cs:151` | `InvariantesDaCarteiraTests.VenderAtivo_RemoveAPosicaoQuandoAQuantidadeZera` | `InvariantesDaCarteiraTests.VenderAtivo_RecusaVendaMaiorQueAPosicaoEPreservaQuantidade` | `requisitos.md` §3 |
| **R3** preço > 0 | `Models/Ativos/Ativo.cs:36,47` | `AtivoTests.AtualizarPreco_RecalculaARentabilidadeAcumulada` | `AtivoTests.AtualizarPreco_RecusaPrecoNaoPositivo` · `AtivoTests.Construtor_RecusaPrecoInicialNaoPositivo` | `requisitos.md` §3 |
| **R4** categoria única por usuário | `Data/Configurations/OrcamentoConfiguration.cs:25` + `Services/OrcamentoService` | `CarteiraApiTests.Orcamento_RenomeiaCategoriaNoPut` | `CarteiraApiTests.Orcamento_RenomearParaCategoriaExistente_Retorna409` | `requisitos.md` §3 |
| **R5** despesa positiva e acumulada | `Models/Orcamento.cs:90-95` | `OrcamentoTests.AdicionarDespesa_AcumulaGastoEAtualizaSaldoRestante` | `OrcamentoTests.AdicionarDespesa_RecusaValorNaoPositivo` | `requisitos.md` §3 |
| **R6** estado da carteira | `Models/CarteiraSimulada.cs` (`GarantirCarteiraAtiva`, `Suspender`, `Reativar`, `Encerrar`) | `EstadosDaCarteiraTests.Reativar_DeSuspensaVoltaParaAtiva` · `StatusDaCarteiraPersistenciaTests.Suspender_GravaEstadoESobreviveATrocaDeContexto` | `EstadosDaCarteiraTests.Reativar_DeEncerrada_EProibido_EMantemEstadoESaldo` · `StatusDaCarteiraPersistenciaTests.TransicaoProibida_NaoAlteraRegistroPersistido` | `requisitos.md` §3-4 |

Contagem de cenários de sucesso/violação das cinco regras: **R1** 3, **R2** 2,
**R3** 3, **R4** 2, **R5** 2 → **12 cenários** (o mínimo exigido é 10).

## 3. Critérios de aceite → evidência

| Critério | Evidência |
|---|---|
| CA-F1.1 | `Compra_AlteraSaldoEPosicaoNoBanco` (saldo e posição conferidos no banco) |
| CA-F1.2 | `CompraSemSaldo_Retorna422` + `ComprarAtivo_RecusaCompraSemSaldoEPreservaEstado` |
| CA-F1.3 | `ComprarAtivo_CalculaPrecoMedioPonderadoAposMultiplasCompras` |
| CA-F1.4 | `CarteiraSuspensa_RecusaCompraVendaEAporte_SemAlterarSaldoNemPosicoes` · `CarteiraSuspensa_Persistida_RecusaCompraSemGravarSaldo` |
| CA-F2.1 | `CompraEVenda_AtualizamSaldoEPosicaoNoBanco` |
| CA-F2.2 | `VenderAtivo_RecusaVendaMaiorQueAPosicaoEPreservaQuantidade` |
| CA-F2.3 | `Venda_ZeraAPosicaoEAumentaOSaldo` |
| CA-F2.4 | `CarteiraEncerrada_RecusaMovimentacao` |
| CA-F3.1 | `AdicionarDespesa_AcumulaGastoEAtualizaSaldoRestante` |
| CA-F3.2 | `AdicionarDespesa_RecusaValorNaoPositivo` |
| CA-F3.3 | `ValidarEstouroOrcamento_FicaVerdadeiroAoUltrapassarOLimite` |
| CA-F3.4 | `Orcamento_IsoladoPorUsuario` |

## 4. Itens da rubrica → documento

| Item | Documento |
|---|---|
| A01–A08 | `modelo-de-classes.md` §2-4, `analise-do-modelo.md` §1-3 |
| B01–B08 | `modelo-de-classes.md`, `analise-do-modelo.md` |
| C01–C06 | `revisao-de-codigo.md` |
| D01–D04 | `requisitos.md` §1 e §3, `auditoria-final.md` §2 |
| E01–E06 | `registro-de-depuracao.md` |
| F01–F06 | este documento, `auditoria-final.md` |
| G01–G02 | `requisitos.md`, `notas/prompts/2026-11-01-prompt_api_financas_Lucas-gomes-01.md` + relatório/log correspondentes |

## 5. Lacunas conscientes desta matriz

1. `Status` da carteira não é exposto em `CarteiraResponse`, então não há critério de
   aceite HTTP para R6 — a prova é de domínio e de persistência.
2. Não há teste de integração HTTP para o fluxo F3 de despesa além de
   `Orcamento_IsoladoPorUsuario`; o restante da cobertura de F3 é de serviço/domínio.
3. Os testes de `Api` usam `WebApplicationFactory` e banco temporário — não cobrem
   o processo publicado (Kestrel real). O smoke manual está descrito em
   `notas/relatorios_prompt/..._log-execucao...md` §7.
