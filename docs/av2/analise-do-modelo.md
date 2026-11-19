# Análise do modelo — invariantes e decisões

Registro das invariantes que o modelo protege e das decisões de modelagem que
exigiram comparação de alternativas. Complementa `modelo-de-classes.md`.

## 1. Invariantes e o objeto responsável (B07)

Invariante é a condição que nunca pode ser falsa em um objeto válido — inclusive
quando ele vem do banco. A coluna "quem protege" indica o **único** ponto que pode
recusar a violação.

| # | Invariante | Quem protege | Como recusa | Teste |
|---|---|---|---|---|
| I1 | O saldo disponível da carteira nunca fica negativo | `CarteiraSimulada.ComprarAtivo` (`Models/CarteiraSimulada.cs:116`) | `InvalidOperationException` antes de debitar | `ComprarAtivo_RecusaCompraSemSaldoEPreservaEstado` |
| I2 | A quantidade de uma posição nunca fica negativa | `Posicao.ReduzirQuantidade` (`Models/CarteiraSimulada.cs`) e `CarteiraSimulada.VenderAtivo:151` | `InvalidOperationException` antes de subtrair | `VenderAtivo_RecusaVendaMaiorQueAPosicaoEPreservaQuantidade` |
| I3 | Carteira `Encerrada` não volta a `Ativa` nem a `Suspensa` | `CarteiraSimulada.Reativar` / `Suspender` (`Models/CarteiraSimulada.cs`) | `InvalidOperationException` antes de trocar o estado | `Reativar_DeEncerrada_EProibido_EMantemEstadoESaldo` |
| I4 | Não existem duas categorias com o mesmo nome para o mesmo usuário | `OrcamentoConfiguration` (índice único) + `OrcamentoService` | `DbUpdateException` de unicidade → `409` | `Orcamento_RenomearParaCategoriaExistente_Retorna409` |
| I5 | Ativo com preço ≤ 0 não existe | `Ativo` (construtor e `AtualizarPreco`, `Models/Ativos/Ativo.cs:36,47`) | `ArgumentException` | `AtivoTests` |

**Por que I3 é a invariante central da AV2.** O estado da carteira deixa de ser um
booleano e passa a ter três valores; a proteção não pode morar no controller (que
seria facilmente contornado por outro chamador) nem no banco (que não expressa
"transição"), e sim no agregado que possui o estado. `Suspender`, `Reativar` e
`Encerrar` são os únicos métodos capazes de escrever `Status`, e todos checam a
origem antes de escrever.

**Atomicidade da recusa.** A checagem e a escrita acontecem dentro do mesmo
`_lock`, e a exceção é lançada **antes** de qualquer atribuição. Consequência
observável: depois de uma transição proibida, o objeto e o registro persistido
continuam no estado anterior — verificado em
`StatusDaCarteiraPersistenciaTests.TransicaoProibida_NaoAlteraRegistroPersistido`.

## 2. Decisão 1 — Estado da carteira: booleano ou estado nomeado?

| Alternativa | Vantagens | Desvantagens |
|---|---|---|
| (a) `bool Ativa` | menor código; migração trivial | não distingue "suspensa" de "encerrada"; `true/false` não expressa estado final; qualquer `false` vira o mesmo caso; transições ilegais ficam indistinguíveis das legais |
| (b) `enum StatusCarteira { Ativa, Suspensa, Encerrada }` | nomeia os três estados; torna explícito o estado final; permite recusar transição por origem; o valor persiste legível (`"Ativa"`) | exige coluna nova, migração e configuração de conversão |

**Escolha implementada: (b).** Motivo técnico: com (a) seria impossível implementar
a recusa de transição — `Encerrada → Ativa` e `Suspensa → Ativa` teriam a mesma
representação, e o teste de transição proibida não teria como distinguir os casos.
O custo aceito foi uma migration (`20261119025829_StatusDaCarteira`) com valor
padrão `"Ativa"`, que preserva as linhas existentes sem backfill manual.

Consequência de projeto: o estado é **interno ao agregado**. Não foi exposto em
`CarteiraResponse` porque o produto não oferece transição pela API hoje; expor um
campo que só assume um valor seria contrato sem uso (ver `requisitos.md` §5).

## 3. Decisão 2 — Rendimento: `switch` no serviço ou método polimórfico?

| Alternativa | Vantagens | Desvantagens |
|---|---|---|
| (a) `switch (ativo.Tipo)` dentro de `AtivoService` | concentra tudo em um arquivo; fácil de ler de imediato | repete a fórmula fora do domínio; cada subtipo novo exige editar o serviço; a regra de rendimento passa a ter dois donos (modelo e serviço); `switch` cresce sem limite |
| (b) `Ativo.CalcularRendimento` abstrato, uma implementação por subtipo | a fórmula mora com o dado que ela usa; subtipo novo entra sem tocar no serviço; o contrato é verificável por teste sobre a referência base | exige classe abstrata e cinco implementações; um subtipo mal implementado só é pego por teste |

**Escolha implementada: (b).** Motivo técnico: rendimento é regra do ativo, não do
catálogo. Com (a), `Acao` e `FundoImobiliario` teriam a fórmula duplicada no
serviço e no modelo — exatamente a duplicação por tipo concreto que o Cap. 12
descreve. Além disso, (b) permite que a aplicação use o contrato sem conhecer o
tipo: `AtivoService.CalcularRendimentoEstimado` chama `ativo.CalcularRendimento` e
não tem `switch`.

**Verificação de que a escolha é real, não decorativa.** Antes desta rodada, o
método polimórfico só era chamado por testes. Agora existe um caminho de produção:
`GET api/ativo/{ticker}/rendimento?quantidade=N` → `AtivoService` → chamada
polimórfica. Teste correspondente:
`RendimentoPolimorficoTests.MesmaChamadaSobreReferenciaBase_RespondeDiferentePorSubtipo`.
