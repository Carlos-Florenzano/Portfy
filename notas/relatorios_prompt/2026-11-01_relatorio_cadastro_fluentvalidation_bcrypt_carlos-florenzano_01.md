# Relatório — Cadastro com FluentValidation e BCrypt

| Item | Valor |
|---|---|
| Repositório | `/home/carlos/Documents/Portfy` |
| Prompt de origem | `notas/prompts/2026-11-01_prompt_sqlite_carlos-florenzano_01.md` |
| Data | 2026-11-01 |
| Escopo | APIs de cadastro de usuário com validação (FluentValidation), hash de senha (BCrypt.Net-Next) e complementos de API (DTOs, mapeamento, tratamento de exceção, injeção de dependência) |
| Estado do Git | Alterações **não commitadas** (nenhum commit, merge ou push foi executado) |

---

## 1. Leitura prévia e decisões de arquitetura

Antes de alterar código foram lidos: `AGENTS.md` (contrato operacional), `README.md`,
`docs/arch.md`, `docs/funcionalidades.md`, `docs/roadmap.md`, `docs/diagrama.md`,
`docs/diagrama-classe-candidatas.md`, `notas/importante/sugestoes_mvp_portfy_temp_outubro_2026.md`,
`notas/importante/portfy_avaliacao_av1_professor_andre.md`, além de todo o backend
(`Controllers/`, `Services/`, `Models/`, `Data/`), dos testes e dos serviços do SPA.

O repositório exige (arch §7 e §19) que toda exceção às decisões vinculantes seja registrada
em ADR. Duas decisões novas foram necessárias e estão registradas em `docs/arch.md` §19:

- **ADR-018** — hash de senha em **BCrypt (fator de trabalho 12)** via `ISenhaHasher`, com
  ponte de verificação para hashes PBKDF2 existentes. Substitui a meta anterior de
  `PBKDF2 ≥ 210.000` iterações (arch §15.3 / dívida P1-d), que era a única forma de atender
  ao pedido explícito do prompt sem bloquear usuários já cadastrados.
- **ADR-019** — validação de forma com **FluentValidation** aplicada por um filtro global,
  em vez de `DataAnnotations` ou `if` no controller (`arch §14.1`).

O hash deixou de morar no modelo, como já previsto em `arch §13.5`, `docs/roadmap.md` E1.1 e
`docs/diagrama-classe-candidatas.md`.

---

## 2. Implementado

### 2.1 Dependências (`Portfy.csproj`)

| Pacote | Versão | Papel |
|---|---|---|
| `FluentValidation` | 12.2.0 | regras de validação dos DTOs |
| `FluentValidation.DependencyInjectionExtensions` | 12.2.0 | descoberta/registro dos validadores por assembly |
| `BCrypt.Net-Next` | 4.0.3 | hash de senha (BCrypt, fator 12) |

### 2.2 DTOs de entrada — `Dtos/Usuario/`

Um arquivo por tipo (convenção `arch §16.1, item 8`), sem dependência de `Models/` (`arch §7.1`):

- `CadastroRequest` — `Nome`, `SalarioMensal`, `Login`, `Senha`
- `LoginRequest` — `Login`, `Senha`
- `AtualizarUsuarioRequest` — `Nome`, `SalarioMensal`
- `AtualizarSalarioRequest` — `NovoSalario`
- `AlterarSenhaRequest` — `SenhaAtual`, `NovaSenha`

### 2.3 DTOs de resposta — `Dtos/Usuario/`

Substituem os `return Ok(new { ... })` do `UsuarioController`, preservando exatamente os nomes
de campo já consumidos pelo SPA (`mensagem`, `id`, `nome`, `login`, `salarioMensal`,
`dataRecebimento`, `contaVerificada`, `salarioAnterior`, `novoSalario`):

- `UsuarioResponse` (leitura: lista e por id; sem `SenhaHash`, conforme arch §10.6)
- `UsuarioConfirmadoResponse` (cadastro e login)
- `VerificacaoContaResponse`
- `AtualizacaoUsuarioResponse`
- `AtualizacaoSalarioResponse`
- `MensagemResponse` (alteração de senha e exclusão)

### 2.4 Validadores FluentValidation — `Dtos/Usuario/*RequestValidator.cs`

| DTO | Regras | Mensagens |
|---|---|---|
| `CadastroRequest` | nome não vazio, 3–200; login não vazio, 3–200; senha 8–64; salário ≥ 0 | pt-BR, por campo |
| `LoginRequest` | login e senha não vazios | não revela a política de senha |
| `AtualizarUsuarioRequest` | nome não vazio, 3–200; salário ≥ 0 | idênticas ao cadastro |
| `AtualizarSalarioRequest` | `NovoSalario > 0` | mantém o comportamento anterior do endpoint |
| `AlterarSenhaRequest` | senha atual não vazia; nova senha 8–64 e diferente da atual | pt-BR |

O limite de 64 caracteres da senha é intencional: o BCrypt considera no máximo 72 **bytes** e
um caractere UTF-8 pode ocupar mais de um byte.

### 2.5 Hash de senha — `Services/Contracts/ISenhaHasher.cs` + `Services/SenhaHasher.cs`

- `CriarHash(senha)` → BCrypt com **fator de trabalho 12** (`2^12` rodadas internas) e salt
  gerado pela própria biblioteca.
- `Verificar(senha, hash)` → aceita o hash BCrypt (60 caracteres, prefixo `$2`) e, para
  compatibilidade, o formato PBKDF2 legado (`salt:hash` em Base64, 100.000 iterações,
  SHA-256, comparação com `CryptographicOperations.FixedTimeEquals`).
- `PrecisaAtualizar(hash)` → indica hash legado; o `UsuarioService` regrava em BCrypt no
  primeiro login bem-sucedido.
- Hash malformado/truncado ou Base64 inválido retorna `false` (credencial inválida), sem
  derrubar a requisição com `500` e sem `catch` vazio.
- Sem estado → registrado como `Singleton`.

### 2.6 Tratamento de exceção — `Middleware/ExceptionHandlingMiddleware.cs`

Política única de erro (arch §14.2 / ADR-005), sem `try/catch` nas actions:

| Exceção | Status | `detail` |
|---|---|---|
| `ArgumentException` / `ArgumentOutOfRangeException` | 400 | mensagem da exceção |
| `KeyNotFoundException` | 404 | mensagem genérica |
| `InvalidOperationException` | 422 | mensagem da exceção |
| `DbUpdateException` com `SQLITE_CONSTRAINT_UNIQUE` (2067) | 409 | mensagem genérica |
| qualquer outra | 500 | mensagem genérica (sem stack trace) |

Detalhes de resiliência: cancelamento do cliente (`OperationCanceledException` +
`RequestAborted`) não é tratado como erro; se a resposta já começou, a exceção não é reescrita;
4xx geram `LogWarning`, 5xx geram `LogError` com a exceção completa. A resposta é escrita
diretamente como `application/problem+json`, sem depender de `IProblemDetailsService`
registrado.

### 2.7 Validação global — `Filters/FluentValidationFilter.cs`

`IAsyncActionFilter` global que, para cada argumento da action, resolve `IValidator<T>` no
container e executa as regras. Em caso de falha, responde `400` com `ValidationProblemDetails`:
`detail` com a primeira mensagem e `errors` por campo (chaves em camelCase, iguais ao JSON).
Isso mantém o 400 automático esperado pelo SPA e concentra a validação de forma em um ponto.

### 2.8 Composição e DI — `Program.cs`

- `AddControllers(options => options.Filters.Add<FluentValidationFilter>())`
- `AddValidatorsFromAssemblyContaining<CadastroRequestValidator>()`
- `AddSingleton<ISenhaHasher, SenhaHasher>()`
- `app.UseMiddleware<ExceptionHandlingMiddleware>()` após `UseCors` e antes de `MapControllers`

### 2.9 Serviço de aplicação — `Services/UsuarioService.cs`

- Recebe `AppDbContext` + `ISenhaHasher` por construtor (Scoped). O seam de teste
  `UsuarioService()` continua existindo (SQLite em memória + `SenhaHasher`).
- `Cadastrar` calcula o hash **antes** de criar o `Usuario`; o modelo nunca vê a senha em texto puro.
- `Autenticar` verifica a senha pelo hasher e regrava o hash quando ele é legado.
- `AlterarSenha` verifica a senha atual pelo hasher e grava o novo hash via domínio.

### 2.10 Domínio — `Models/Usuario.cs`

- Construtores com login passaram a receber **`senhaHash`** em vez de senha.
- Novo método de domínio `AlterarSenhaHash(string novoHash)` (valida hash não vazio).
- Invariantes preservadas: `id >= 0`, nome não vazio, login não vazio, salário ≥ 0.

### 2.11 Testes (`tests/Portfy.Tests`)

Novos: `Services/SenhaHasherTests.cs` (6), `Dtos/CadastroRequestValidatorTests.cs` (7),
`Dtos/AlterarSenhaRequestValidatorTests.cs` (4), `Middleware/ExceptionHandlingMiddlewareTests.cs` (4).
Atualizados: `Models/UsuarioTests.cs`, `Services/UsuarioServiceTests.cs`,
`Data/PersistenciaTests.cs` (inclui o caso de **migração de hash legado** para BCrypt).

Total: **89 casos**, 0 falhas (antes: 66).

### 2.12 Documentação atualizada

- `docs/arch.md`: stack (§5.1), pasta `Filters/` (§6.1/§8.1), validação de forma (§14.1),
  segurança de senhas (§15.3), baseline de testes (§4/§18.2), ADR-018/ADR-019 (§19) e dívidas
  P1-c/P1-d (§20).
- `docs/roadmap.md`: E1.1 concluída (BCrypt), E0.1/E0.2 marcadas como parciais (recurso usuário),
  estado de partida atualizado.
- `docs/funcionalidades.md`: cadastro, login, salário e troca de senha; envelope de erro;
  cobertura de testes.
- `docs/diagrama.md` e `docs/diagrama-classe-candidatas.md`: hash BCrypt, classes que passaram
  de `criar` para `existe`.

---

## 3. Removido

| Item | Motivo |
|---|---|
| `Usuario.CriarHashSenha` (PBKDF2/SHA-256) | hash sai do modelo (arch §13.5); substituído por `SenhaHasher` |
| `Usuario.VerificarSenha` | verificação exige o hasher; passou para `UsuarioService` + `ISenhaHasher` |
| `Usuario.VerificarLogin` | não tinha consumidor e duplicava a verificação |
| `Usuario.AlterarSenha(senhaAtual, novaSenha)` | substituído por `AlterarSenhaHash(novoHash)`; a checagem da senha atual é regra de aplicação |
| `using System.Security.Cryptography` em `Models/Usuario.cs` | sem uso após a extração do hash |
| DTOs aninhados (`CadastroRequest`, `LoginRequest`, ...) em `UsuarioController` | proibidos por arch §6.2 / ADR-004 |
| `try/catch (ArgumentException)` do `UsuarioController` (4 blocos) | centralizados no middleware (arch §14.2 / ADR-005) |
| Validações manuais de campo no `Cadastrar` | substituídas pelos validadores FluentValidation |
| Respostas anônimas (`new { ... }`) do `UsuarioController` | substituídas por DTOs tipados |

---

## 4. Modificado

| Arquivo | Mudança |
|---|---|
| `Portfy.csproj` | 3 novos `PackageReference` (FluentValidation, extensões DI, BCrypt.Net-Next) |
| `Program.cs` | filtro de validação, registro dos validadores, `ISenhaHasher` e middleware |
| `Controllers/UsuarioController.cs` | DTOs externos, respostas tipadas, `Problem(...)` para 409/401/404, sem `try/catch` |
| `Services/UsuarioService.cs` | injeção de `ISenhaHasher`; hash no cadastro; rehash na autenticação; verificação na troca de senha |
| `Models/Usuario.cs` | construtores recebem hash; `AlterarSenhaHash`; sem criptografia |
| `Data/Configurations/UsuarioConfiguration.cs` | comentário do mapeamento alinhado a BCrypt (schema e tamanho da coluna inalterados) |
| `tests/Portfy.Tests/*` | ajustes de construtores e novos testes (ver §2.11) |
| `docs/*` | ver §2.12 |

Nenhuma migration foi necessária: o schema não mudou (a coluna `SenhaHash TEXT(400)` comporta
tanto BCrypt quanto o hash legado).

---

## 5. Contrato HTTP resultante

| Verbo | Rota | Corpo | Sucesso | Erros |
|---|---|---|---|---|
| POST | `api/Usuario/cadastro` | `CadastroRequest` | `201` + `Location` + `UsuarioConfirmadoResponse` | `400` (validação), `409` (login duplicado) |
| POST | `api/Usuario/login` | `LoginRequest` | `200` + `UsuarioConfirmadoResponse` | `400`, `401` |
| GET | `api/Usuario` | — | `200` + `UsuarioResponse[]` | — |
| GET | `api/Usuario/{id}` | — | `200` + `UsuarioResponse` | `404` |
| PUT | `api/Usuario/{id}` | `AtualizarUsuarioRequest` | `200` + `AtualizacaoUsuarioResponse` | `400`, `404` |
| PUT | `api/Usuario/{id}/salario` | `AtualizarSalarioRequest` | `200` + `AtualizacaoSalarioResponse` | `400`, `404` |
| PUT | `api/Usuario/{id}/senha` | `AlterarSenhaRequest` | `200` + `MensagemResponse` | `400`, `404` |
| POST | `api/Usuario/{id}/verificar` | — | `200` + `VerificacaoContaResponse` | `404` |
| DELETE | `api/Usuario/{id}` | — | `200` + `MensagemResponse` | `404` |

As rotas em PascalCase foram preservadas de propósito (a padronização é a dívida P2-f, fora do
escopo deste prompt). O SPA **não precisou de alteração**: os nomes de campo das respostas foram
mantidos e o interceptor `services/api.ts` já lê `ProblemDetails.detail`.

---

## 6. Gates executados (evidências)

| Gate | Comando | Resultado |
|---|---|---|
| Build do backend | `dotnet build Portfy.csproj` | **PASSA** — 0 erros, 0 warnings |
| Testes | `dotnet test tests/Portfy.Tests/Portfy.Tests.csproj` | **PASSA** — 89/89, 0 falhas |
| Build do frontend | `cd frontend && npm run build` | **PASSA** — 79 módulos (nenhum arquivo do SPA foi alterado) |
| Lint do frontend | `cd frontend && npm run lint` | **PASSA** — 0 erros |
| `get; set;` em `Models/` | `grep -rn "get; set;" Models` | **vazio** |
| Estado estático de domínio | `grep -rn "private static" Controllers Services` | **vazio** |
| PBKDF2 em `Models/` | `grep -rn "Rfc2898DeriveBytes" Models` | **vazio** |
| `EnsureCreated` chamado | `grep -rn "\.EnsureCreated(" --include="*.cs" .` | **vazio** |

### 6.1 Smoke real da API (servidor em `127.0.0.1:5199`, banco descartável)

| Requisição | Resposta observada |
|---|---|
| `POST /api/usuario/cadastro` inválido (`nome="  "`, `salario=-5`, `login="ab"`, `senha="123"`) | `400` `application/problem+json`, `errors` por campo (`nome`, `salarioMensal`, `login`, `senha`) |
| `POST /api/usuario/cadastro` válido | `201` + `Location: /api/Usuario/1` + `{mensagem, id, nome, login, contaVerificada}` |
| `POST /api/usuario/cadastro` com login duplicado (caixa diferente) | `409` + `detail: "Esse login já está cadastrado."` |
| `POST /api/usuario/login` antes da verificação | `401` + `detail: "A conta ainda não foi verificada."` |
| `POST /api/usuario/1/verificar` e novo login | `200` (BCrypt verificado) |
| `POST /api/usuario/login` com senha errada | `401` + `detail: "Login ou senha incorretos."` |
| `PUT /api/usuario/1/senha` com senha atual errada | `400` + `detail: "A senha atual informada está incorreta."` (middleware, sem `try/catch`) |
| `GET /api/usuario/1` | `200` + `UsuarioResponse` (sem `senhaHash`) |
| `GET /api/usuario/999` | `404` + `ProblemDetails` |
| `GET /api/ativo`, `/api/carteira`, `/api/orcamento` | `200` (controllers não alterados continuam funcionando) |

O banco criado para o smoke (`portfy.db`) foi removido ao final; ele é ignorado pelo Git
(`.gitignore` linhas 12–14).

---

## 7. Pendências e riscos conhecidos

1. **Demais controllers** (`Ativo`, `Carteira`, `Orçamento`, `Aporte`) ainda devolvem objetos
   anônimos, mantêm DTOs aninhados e `try/catch` próprios. O middleware e o filtro já os
   alcançam, mas a migração de contrato desses recursos é E0.1/E0.2 do roadmap e ficou fora
   deste prompt para não alterar contratos consumidos pelo Dashboard sem necessidade.
2. **Login sem rate limiting** (P1-g) e **sem emissão de token** (P1-a/ADR-010) continuam
   pendentes; o cadastro agora entrega credencial bem protegida, mas a sessão continua sendo
   apenas `localStorage` no SPA.
3. **Hash legado** permanece apenas como caminho de verificação; depois que todos os usuários
   fizerem login, o código de PBKDF2 pode ser removido em uma etapa própria.
4. O fator de trabalho 12 do BCrypt foi escolhido como padrão de segurança; se o tempo de
   login (~250 ms por verificação nesta máquina) virar problema de UX, o valor é um único
   `const` em `SenhaHasher`, mas reduzi-lo exige justificativa registrada.
5. Nenhum commit foi criado: o prompt trata o commit como opcional e o contrato do repositório
   proíbe operações de VCS sem autorização explícita.

---

## 8. Como reproduzir a validação

```bash
cd /home/carlos/Documents/Portfy
dotnet build Portfy.csproj
dotnet test tests/Portfy.Tests/Portfy.Tests.csproj
cd frontend && npm run build && npm run lint
```

Smoke manual (em um terminal):

```bash
cd /home/carlos/Documents/Portfy
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Portfy.csproj
# em outro terminal:
curl -i -X POST http://localhost:5000/api/usuario/cadastro \
  -H "Content-Type: application/json" \
  -d '{"nome":"Ana Souza","salarioMensal":3000,"login":"ana@portfy.test","senha":"segredo123"}'
```
