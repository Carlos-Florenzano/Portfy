# Revisão de código — alteração funcional `fe92182`

Análise de uma alteração real do histórico Git, com diff verificável, inventário de
arquivos, impacto e parecer. Este documento é uma inspeção: descreve o que o
commit faz, não o que se gostaria que fizesse.

## 1. Identificação da alteração (C01)

| Campo | Conteúdo |
|---|---|
| Hash | `fe921821d67a9f14efcad1e2bee2f559fdadb2b8` (curto: `fe92182`) |
| Autor | Sérgio Lucas Gomes da Silva Rodrigues |
| Data | 2026-11-03 |
| Mensagem | "Acertando o backend conforme o arquivo de sugestão do professor para AV2" |
| Diff | `git show fe92182` |
| Tamanho | 10 arquivos, 819 inserções, 0 remoções de código |

O diff é verificável a qualquer momento com o comando acima; o commit é ancestral
de `origin/main` (`git merge-base --is-ancestor fe92182 origin/main` → verdadeiro).

## 2. Requisito e regra de negócio correspondentes (C02)

**Requisito.** Um endpoint anônimo que executa trabalho de CPU por chamada precisa
ser limitado por origem. `POST api/usuario/cadastro` é `[AllowAnonymous]` e cada
chamada executa `BCrypt` (fator de trabalho 12). Sem limite, um cliente satura a CPU
do servidor com cadastros em massa.

**Regra implementada.** "Cadastro aceita no máximo 10 requisições por minuto por
IP; a 11ª recebe `429` com `Retry-After`." Não é regra de domínio financeiro — é
regra de proteção do serviço, aplicada na borda HTTP (por desenho, não pertence a
`Models/`).

**Prova de que o código implementou o comportamento:**

- `Services/Contracts/PoliticasDeRateLimit.cs` — nova constante `Cadastro`;
- `Program.cs` — política `Cadastro` com `FixedWindowRateLimiter`, `PermitLimit = 10`, janela de 1 minuto, partição por `RemoteIpAddress`;
- `Controllers/UsuarioController.cs` — atributo `[EnableRateLimiting(PoliticasDeRateLimit.Cadastro)]` na action de cadastro;
- `tests/Portfy.Tests/Api/CadastroRateLimitTests.cs` — `Cadastro_AcimaDoLimitePorMinuto_Retorna429ComRetryAfter`.

## 3. Inventário dos arquivos alterados (C03)

| Arquivo | +linhas | Finalidade | Está no escopo? |
|---|---|---|---|
| `Services/Contracts/PoliticasDeRateLimit.cs` | +5 | nome canônico da política; evita string literal repetida | **sim** — é o requisito |
| `Program.cs` | +13 | registra a política `Cadastro` com janela própria | **sim** |
| `Controllers/UsuarioController.cs` | +1 | aplica a política na action de cadastro | **sim** |
| `tests/Portfy.Tests/Api/CadastroRateLimitTests.cs` | +36 | prova o `429` e o `Retry-After` | **sim** — teste da regra |
| `tests/Portfy.Tests/Api/AutorizacaoApiTests.cs` | +249 | cobre autorização/IDOR/JWT adulterado | **parcialmente** — mesmo objetivo (AV2), outro requisito |
| `tests/Portfy.Tests/Models/InvariantesDaCarteiraTests.cs` | +180 | cobre R1, R2, R5 e o contrato polimórfico | **parcialmente** — idem |
| `tests/Portfy.Tests/Services/JwtConfiguracaoTests.cs` | +124 | valida a configuração de JWT | **parcialmente** |
| `notas/relatorios_prompt/..._05.md` | +164 | relatório do próprio trabalho | **sim** — processo do grupo |
| `notas/relatorios_prompt/..._04.md` | +47 | relatório de trabalho anterior | **fora do escopo** |
| `notas/relatorios_prompt/2026-11-02_..._03.md → 2026-11-03_..._03.md` | 0 | **renomeação** de arquivo de relatório | **fora do escopo** |

**Mudança fora do escopo identificada:** a renomeação de um relatório de outra
data (`2026-11-02` → `2026-11-03`) não tem relação com rate limiting nem com a
regra de cadastro. É rastreável (o Git mostra `R100`/0 linhas) e não altera
conteúdo, mas mistura processo com produto no mesmo commit.

## 4. Impacto sobre regras, contratos e dependências (C04)

| Dimensão | Impacto | Verificação |
|---|---|---|
| Contrato HTTP | **nenhum** campo, rota ou status alterado; a rota passa a poder responder `429` | `CadastroRateLimitTests` |
| Modelo de domínio | **nenhum** — `Models/` não foi tocado | `git show --numstat fe92182` |
| Persistência | **nenhum** — `Data/` e migrations não foram tocados | idem |
| Configuração | nova política no `RateLimiter`; `Login` e `Mercado` preservadas | leitura de `Program.cs` |
| Dependências | nenhum pacote novo | `.csproj` inalterado |
| Comportamento de erro | `429` não passa pelo `ExceptionHandlingMiddleware` (é resposta do próprio rate limiter, não exceção) | teste de borda |

**Verificação executada nesta revisão (2026-11-19):**

```
dotnet build Portfy.csproj      → 0 erros, 0 avisos
dotnet test tests/Portfy.Tests  → 247 aprovados, 0 falhas
```

Observação de ambiente: `dotnet build` direto falha nesta máquina por
`NuGet.targets(782,5): Value cannot be null (Parameter 'path1')` — reproduzido
também em um projeto vazio, logo é ambiente, não repositório. O contorno está
documentado em `notas/relatorios_prompt/2026-11-01_relatorio_log-execucao-api-financas_lucas-gomes_01.md` §9.2.

## 5. Avaliação por propriedades de POO (C05)

| Propriedade | Efeito da alteração | Evidência |
|---|---|---|
| **Coesão** | melhorou: a política de cadastro ficou junto das demais políticas em `PoliticasDeRateLimit`, em vez de um literal no atributo | `PoliticasDeRateLimit.cs` |
| **Acoplamento** | controlado: o controller depende de **uma constante** (`PoliticasDeRateLimit.Cadastro`) e não conhece limites nem janela; trocar 10/min exige editar só `Program.cs` | `UsuarioController.cs` + `Program.cs` |
| **Encapsulamento** | preservado: a configuração do limiter não vaza para a action; o controller só declara qual política usa | `[EnableRateLimiting(...)]` |
| **Responsabilidade** | respeitada: a decisão de "quanto é demais" fica na composição da aplicação (`Program.cs`), não na regra de negócio do usuário | `Program.cs` |

Nenhuma dessas propriedades foi **prejudicada**. O ponto negativo é de disciplina
de commit, não de design: o commit mistura três frentes (rate limit, autorização,
invariantes) e uma renomeação.

## 6. Parecer técnico (C06)

**Decisão: ACEITO, com ressalva de escopo.**

*Fatos que sustentam o aceite:*

1. o diff implementa exatamente a regra declarada;
2. há teste automatizado que falha se o `429` deixar de existir;
3. nenhum contrato HTTP, modelo ou migration foi alterado;
4. build e suíte completos passam (247/247).

*Ressalvas registradas (não bloqueiam o aceite):*

1. o commit reúne assuntos distintos — o certo seria um commit só para o rate
   limit e outro para os testes de autorização/invariantes;
2. a renomeação de relatório de outra data é mudança fora do escopo;
3. o limite é por IP e, atrás de proxy reverso, todos os clientes compartilhariam
   a mesma partição — comportamento falha-fechado (protege o servidor), mas deve
   ser revisto antes de produção.

*O que NÃO foi aceito sem prova:* a afirmação de que a suíte inteira continuava
verde. Ela foi **reexecutada** nesta revisão, não aceita por leitura do relatório.
