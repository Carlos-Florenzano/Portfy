# Auditoria final — Portfy × rubrica AV2

Auditoria dos 40 itens da rubrica contra o repositório. Cada linha declara o que foi
**comprovado**, por qual evidência, e o que **não** foi comprovado. Nada aqui é
declarado concluído por ter sido planejado.

## 1. Gates executados (2026-11-19)

| Gate | Comando | Resultado |
|---|---|---|
| Build | `dotnet build Portfy.csproj` | **0 erros, 0 avisos** |
| Testes | `dotnet test tests/Portfy.Tests` | **247 aprovados, 0 falhas, 0 ignorados** |
| Clone limpo | `git archive HEAD` → restore → build → test | **247 aprovados** (simulação de clone) |
| Reprodução de defeito | `dotnet test --filter AutenticacaoTests` sem `Pooling=False` | **5 falhas** (antes) → **5 aprovadas** (depois) |
| Estado do repositório | `git diff HEAD origin/main` | **vazio** — a árvore auditada é a entregue |
| Smoke HTTP real | API em `127.0.0.1:5199`, `GET api/ativo/{ticker}/rendimento` | **200** `3.85` (PETR4/Acao) · **200** `11.0` (HGLG11/Fii) · **400** quantidade 0 · **404** ticker inexistente |
| Migração em banco existente | `PRAGMA table_info(Carteiras)` após o startup | coluna `Status` criada; linha existente migrada como `Ativa` sem backfill |

Observação de ambiente: `dotnet build` sem as variáveis de pasta do Windows falha
com `NuGet.targets(782,5): Value cannot be null (Parameter 'path1')`. Reproduzido
também em projeto vazio ⇒ é ambiente, não repositório. Contorno documentado em
`notas/relatorios_prompt/2026-11-01_relatorio_log-execucao-api-financas_lucas-gomes_01.md` §9.2.

## 2. Resultado por item

Legenda: **A** = atendido com evidência · **P** = parcial (evidência incompleta para o texto do item).

| Item | Situação | Evidência principal |
|---|---|---|
| A01 classes de domínio ≥ 5 | **A** | `Usuario`, `CarteiraSimulada`, `Posicao`, `Ativo`+5 subtipos, `AporteSimulado`, `Orcamento` |
| A02 ≥ 2 classes com comportamento próprio | **A** | `CarteiraSimulada.ComprarAtivo/VenderAtivo`, `Ativo.AtualizarPreco/CalcularRendimento`, `Orcamento.AdicionarDespesa` |
| A03 ≥ 5 regras usadas no backend | **A** | R1–R5 em `requisitos.md` §3, chamadas por controllers reais |
| A04 fluxo com colaboração entre classes | **A** | `ComprarAtivo` colabora `CarteiraSimulada` + `Posicao` + `Ativo` |
| A05 objeto com 3 estados e transições | **A** | `StatusCarteira`, `Suspender/Reativar/Encerrar`, `EstadosDaCarteiraTests` |
| A06 transição proibida recusada sem inconsistência | **A** | `Reativar_DeEncerrada_EProibido_EMantemEstadoESaldo`, `TransicaoProibida_NaoAlteraRegistroPersistido` |
| A07 ≥ 2 regras protegidas por encapsulamento | **A** | `private set` em `Models/`; coleções `IReadOnlyCollection`; guardas de estado |
| A08 polimorfismo com uso efetivo | **A** | `AtivoService.CalcularRendimentoEstimado` + `GET api/ativo/{ticker}/rendimento`; `RendimentoPolimorficoTests` |
| B01 modelo textual com ≥ 5 classes | **A** | `modelo-de-classes.md` §1 |
| B02 ≥ 5 classes do modelo no código C# | **A** | `modelo-de-classes.md` §2 (tabela arquivo ↔ classe) |
| B03 atributos/métodos sem contradição | **A** | `modelo-de-classes.md` §3; contradições antigas corrigidas (`VerificarLogin`, `Models/Posicao.cs`) |
| B04 associação com multiplicidade implementada | **A** | `Posicao 0..* → 1 Ativo` (FK `AtivoId`, `Restrict`); `modelo-de-classes.md` §5 |
| B05 composição com ciclo de vida | **A** | `CarteiraSimulada *-- Posicao` (`_posicoes` + `Cascade`) |
| B06 contrato polimórfico + 2 implementações no modelo | **A** | `modelo-de-classes.md` §4 (5 implementações) |
| B07 invariante + objeto responsável | **A** | `analise-do-modelo.md` §1 (I1–I5) |
| B08 duas decisões comparando alternativas | **A** | `analise-do-modelo.md` §2–3 |
| C01 alteração no Git com diff | **A** | `fe92182` (`git show fe92182`), `revisao-de-codigo.md` §1 |
| C02 alteração corresponde a requisito | **A** | rate limit de cadastro; `revisao-de-codigo.md` §2 |
| C03 análise dos arquivos e fora do escopo | **A** | `revisao-de-codigo.md` §3 (renomeação identificada como fora do escopo) |
| C04 análise de impacto + verificação | **A** | `revisao-de-codigo.md` §4 (build + 247 testes) |
| C05 ≥ 2 propriedades de POO | **A** | `revisao-de-codigo.md` §5 (coesão, acoplamento, encapsulamento, responsabilidade) |
| C06 parecer de aceitar/corrigir/rejeitar | **A** | `revisao-de-codigo.md` §6 (ACEITO com ressalvas) |
| D01 três situações de falha diferenciadas | **A** | `docs/arch.md` §14.1 (forma / negócio / invariante) + §14.2 (mapa de status) |
| D02 caminho reproduzível de propagação | **A** | `ExceptionHandlingMiddleware` único; `ExceptionHandlingMiddlewareTests` |
| D03 tratamento sem ocultar nem vazar | **A** | `ExcecaoDesconhecida_Vira500SemVazarDetalheInterno`; log com `requisicao_id` |
| D04 testes das três situações | **A** | 5 testes de middleware + `CompraSemSaldo_Retorna422` + validadores |
| E01 defeito reproduzível com evidência | **A** | `registro-de-depuracao.md` §1–2 (reproduzido nesta rodada) |
| E02 condições, esperado e observado | **A** | idem §2 |
| E03 hipótese + investigação | **A** | idem §3 (H1–H4, duas descartadas com evidência) |
| E04 técnica de depuração comprovada | **A** | idem §4 (stack trace + isolamento por filtro) |
| E05 causa, correção e eliminação | **A** | idem §5–6 (5 falhas → 0) |
| E06 antes/depois + comparação de técnicas | **A** | idem §6 (tabela comparativa com breakpoint) |
| F01 5 regras × sucesso/violação (≥10) | **A** | `rastreabilidade.md` §2 — 12 cenários |
| F02 teste de invariante com objeto válido | **A** | `ComprarAtivo_RecusaCompraSemSaldoEPreservaEstado`, `VenderAtivo_...EPreservaQuantidade`, `Reativar_DeEncerrada_...` |
| F03 teste de integração com API e persistência | **A** | `Compra_AlteraSaldoEPosicaoNoBanco`, `Venda_ZeraAPosicaoEAumentaOSaldo` |
| F04 compila e testa em clone limpo | **A** | build + 247 testes verificados em export limpo; instruções em `README.md` → "Como executar", conferidas na prática (API em `localhost:5000`, Swagger só em Development, `GET /api/ativo` = 200, `npm run build` = OK) |
| F05 rastreabilidade fluxos/regras/testes | **A** | `rastreabilidade.md` §1–3 |
| F06 auditoria final sem declarar o não comprovado | **A** | este documento |
| G01 especificação verificável dos 3 fluxos e 5 regras | **A** | `requisitos.md` §2–3 (critérios CA-F1..CA-F3) |
| G02 especificação para o DsCode com evidência | **A** | `notas/prompts/2026-11-01-prompt_api_financas_Lucas-gomes-01.md` + relatório + log de execução |

**Resultado: 40 atendidos + 0 parciais.**

## 3. Pendências e limitações (declaradas, não escondidas)

1. **Bloqueio de identificação.** O PDF exige a seção `## Integrantes` no `README.md`;
   o arquivo tem `## Integrantes do Grupo`. Se a checagem for literal, a nota é **0**
   independentemente dos 40 itens. **Não foi corrigido nesta rodada por decisão
   explícita do grupo.**
2. **Instruções de execução.** A seção "Como executar" do `README.md` foi escrita e
   conferida na prática: API sobe em `localhost:5000` sem `launchSettings`, exige
   `Auth__Jwt__Chave`, expõe Swagger só em `Development`, `GET /api/ativo` responde
   `200` e `npm run build` conclui. Resta apenas o risco do título da seção de
   integrantes (item 1).
3. **`Status` não é exposto na API.** O ciclo de vida existe no domínio e no banco,
   mas não há endpoint de suspender/reativar/encerrar, e `CarteiraResponse` não
   traz o campo. Consequência: R6 é provada por teste de domínio/persistência, não
   por contrato HTTP.
4. **Novo endpoint sem consumo no SPA.** `GET api/ativo/{ticker}/rendimento` é
   backend-only. A regra do repositório (`docs/arch.md` §10.7) exige espelho no
   frontend somente para DTO **consumido pelo SPA** — não é o caso.
5. **Cobertura de F3 é majoritariamente de serviço/domínio.** O teste HTTP de
   orçamento cobre isolamento por usuário, não todos os critérios CA-F3.
6. **Nada foi commitado.** As alterações desta rodada estão no working tree; a
   branch padrão (`origin/main`) só receberá os itens depois do commit.
7. **O documento `docs/funcionalidades.md` foi atualizado** (contagem de testes e
   correção da afirmação sobre `Models/Posicao.cs`), mas outros relatórios antigos
   em `notas/` continuam com números da época em que foram escritos — são registros
   históricos e não foram reescritos.

## 4. O que foi alterado nesta rodada

| Arquivo | Natureza |
|---|---|
| `Models/StatusCarteira.cs` | **novo** — enum de 3 estados |
| `Models/CarteiraSimulada.cs` | `Status`, `Suspender/Reativar/Encerrar`, `GarantirCarteiraAtiva` |
| `Data/Configurations/CarteiraSimuladaConfiguration.cs` | mapeamento texto + valor padrão |
| `Data/Migrations/20261119025829_StatusDaCarteira.*` | **novo** — coluna `Status` |
| `Services/RendimentoEstimado.cs` | **novo** — resultado do rendimento |
| `Services/Contracts/IAtivoService.cs` | método `CalcularRendimentoEstimado` |
| `Services/AtivoService.cs` | chamada polimórfica a `CalcularRendimento` |
| `Dtos/Ativo/RendimentoEstimadoResponse.cs` | **novo** — contrato de resposta |
| `Controllers/AtivoController.cs` | rota `GET {ticker}/rendimento` |
| `tests/.../Models/EstadosDaCarteiraTests.cs` | **novo** — 11 testes |
| `tests/.../Data/StatusDaCarteiraPersistenciaTests.cs` | **novo** — 4 testes |
| `tests/.../Services/RendimentoPolimorficoTests.cs` | **novo** — 6 testes |
| `tests/.../Api/RendimentoApiTests.cs` | **novo** — 4 testes |
| `docs/av2/*` | **novo** — 8 artefatos de avaliação |
| `docs/diagrama-classe-candidatas.md`, `docs/funcionalidades.md`, `docs/diagrama.md` | correção de contradições e contagens |
| `README.md` | **nova seção "Como executar"** (pré-requisitos, backend, frontend, verificação rápida, problema comum de NuGet); seção de integrantes preservada |

Contagem de testes: **222 → 247** (+25).

## 5. Leitura da nota

Com 40 itens atendidos, a faixa aritmética é **10,00** — mas **apenas se o bloqueio do
README for resolvido**. Mantido o título `## Integrantes do Grupo`, a nota é **0** pela
regra do próprio PDF, qualquer que seja o número de itens atendidos. O julgamento dos
itens de modelagem (B, C) é do avaliador; esta auditoria apresenta a evidência, não a
substitui.
