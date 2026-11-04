## Auditoria da nota AV1 (Portfy)
     - [x] Baseline: build 0 avisos/0 erros, 181/181 testes
     - [x] Régua A–H auditada contra o código (doc de avaliação estava desatualizado)
     - [x] Segurança testada: autorização, IDOR, JWT adulterado, injeção SQL, CORS, Swagger, rate limit
     - [x] Lacuna real corrigida: cadastro anônimo sem rate limit (BCrypt = DoS de CPU)
     - [x] 41 testes de regressão adicionados (invariantes, polimorfismo, JWT, autorização, rate limit)
     - [x] Gates finais: build limpo + 222/222 aprovados; artefatos temporários removidos
     - [x] Relatório final com lacunas não corrigidas e limite aritmético da nota

 ◆ # Auditoria da AV1 — Portfy

   ## 1. Constatação principal: o documento de avaliação está desatualizado

   O portfy_avaliacao_av1_professor_andre.md (nota 6,92) descreve um snapshot antigo do repositório. Verifiquei cada afirmação contra o código atual:

   ┌──────────────────────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────────────────────────────────────┐
   │ Afirmação da avaliação                                           │ Estado real verificado                                                                       │
   ├──────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
   │ "Nenhuma interface e nenhuma herança"                            │ **14 interfaces** em `Services/Contracts/` + `Ativo` **abstrata** com 5 subclasses           │
   ├──────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
   │ "Sem injeção de dependência"                                     │ `Program.cs` registra `AddScoped`/`AddSingleton` para todos os serviços                      │
   ├──────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
   │ "`UI/Dashboard.cs` com 701 linhas, órfão"                        │ **Pasta `UI/` não existe**; nenhum `Console.` no backend                                     │
   ├──────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
   │ "Nenhum teste"                                                   │ **222 testes** aprovados (181 antes desta sessão)                                            │
   ├──────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
   │ "Estado global `private static readonly CarteiraSimulada`"       │ **Zero** estado estático mutável (só mappers `static`)                                       │
   ├──────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
   │ Frontend "tratado com `alert`/`console.error`, sem token/sessão" │ **Zero** `alert(`/`console.error`; `api.ts` tem interceptor com `Bearer` e tratamento de 401 │
   └──────────────────────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────────────────────────────────────┘

   Conclusão: os itens que derrubaram a nota já foram corrigidos antes desta sessão. O documento não reflete o código.

   ## 2. Régua A–H — requisito por requisito

   ┌───────────────────────────┬─────────────┬────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┬──────────┐
   │ Dim.                      │ Nota antiga │ Evidência atual                                                                                                                                    │ Situação │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **A** Modelagem (10%)     │ 7,0         │ `Ativo` abstract + `Acao`/`FundoImobiliario`/`Cripto`/`Etf`/`RendaFixa`; `CarteiraSimulada`, `Posicao`, `AporteSimulado`, `Orcamento`, `Usuario`   │ ✅       │
   │                           │             │ com identidade e FK                                                                                                                                │          │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **B** Estado/construtor   │ 9,0         │ **0** ocorrências de `{ get; set; }` em `Models/`; só `private set`/`get;`; construtores validados                                                 │ ✅       │
   │ (15%)                     │             │                                                                                                                                                    │          │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **C** Encapsulamento      │ 9,0         │ Listas privadas expostas como `IReadOnlyCollection`; invariantes validadas em `ComprarAtivo`, `VenderAtivo`, `ReduzirQuantidade`,                  │ ✅       │
   │ (20%)                     │             │ `AdicionarAporte`                                                                                                                                  │          │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **D** Coesão (20%)        │ 5,5         │ Controllers finos (traduzem HTTP), serviços com uma responsabilidade, `Dashboard.cs`/`RelatorioFinanceiro.cs` removidos                            │ ✅       │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **E** Colaboração (15%)   │ 8,0         │ `CarteiraSimulada` **agrega** `Posicao` (cria/remove ao zerar) e **compõe** `AporteSimulado`; `Posicao` referencia `Ativo`                         │ ✅       │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **F** Abstração (10%)     │ 2,0         │ 14 interfaces + classe abstrata; controllers dependem só de contratos                                                                              │ ✅       │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **G** Polimorfismo (5%)   │ 2,0         │ `CalcularRendimento` abstrato com 5 implementações — **agora provado via referência `Ativo`** em teste                                             │ ✅       │
   ├───────────────────────────┼─────────────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼──────────┤
   │ **H** Testes (5%)         │ 0,0         │ 222 testes, incluindo as invariantes exatas pedidas na avaliação                                                                                   │ ✅       │
   └───────────────────────────┴─────────────┴────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┴──────────┘

   ## 3. O que alterei (só backend)

   Correção de vulnerabilidade real — cadastro anônimo sem rate limit:

   ┌──────────────────────────────────────────────┬────────────────────────────────────────────────────────────────────────────────────────┐
   │ Arquivo                                      │ Alteração                                                                              │
   ├──────────────────────────────────────────────┼────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Services/Contracts/PoliticasDeRateLimit.cs` │ nova política `Cadastro`                                                               │
   ├──────────────────────────────────────────────┼────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Program.cs`                                 │ política de 10 cadastros/min por IP (janela própria, não consome o orçamento do login) │
   ├──────────────────────────────────────────────┼────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Controllers/UsuarioController.cs`           │ `[EnableRateLimiting(PoliticasDeRateLimit.Cadastro)]` em `Cadastrar`                   │
   └──────────────────────────────────────────────┴────────────────────────────────────────────────────────────────────────────────────────┘

   Motivo: cada cadastro executa BCrypt custo 12. Sem limite, um cliente anônimo satura a CPU com cadastros em massa. Login já tinha limite; cadastro não.

   41 testes de regressão novos (nenhum teste existente alterado):

   ┌────────────────────────────────────────┬───────┬────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
   │ Arquivo                                │ Casos │ Cobre                                                                                                                                      │
   ├────────────────────────────────────────┼───────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Models/InvariantesDaCarteiraTests.cs` │ 13    │ compra sem saldo, venda sem posição, aporte nulo, redução maior que a quantidade, preço médio ponderado, patrimônio, polimorfismo via base │
   ├────────────────────────────────────────┼───────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Services/JwtConfiguracaoTests.cs`     │ 9     │ chave ausente/curta, validade inválida, padrões, hash não-BCrypt, faixa do BCrypt                                                          │
   ├────────────────────────────────────────┼───────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Api/AutorizacaoApiTests.cs`           │ 18    │ 401 sem token (6 rotas), 403/404 entre usuários, JWT adulterado, `alg:none`, SQLi no login e no ticker, corpo malformado                   │
   ├────────────────────────────────────────┼───────┼────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Api/CadastroRateLimitTests.cs`        │ 1     │ 10×201 e depois 429 + `Retry-After`                                                                                                        │
   └────────────────────────────────────────┴───────┴────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

   ## 4. Teste de vulnerabilidades — resultado empírico

   Sonda contra a API real em ambiente Production (banco temporário, porta isolada, removida ao final):

   ┌──────────────────────────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────┐
   │ Ataque testado                                                               │ Resultado                                           │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Rotas financeiras sem token (`carteira`, `relatorio`, `orcamento`, `aporte`) │ **401** ✅                                          │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Token adulterado / `alg: none` / lixo                                        │ **401** ✅                                          │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Cross-user em `/api/usuario/{id}`                                            │ **403** ✅                                          │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ IDOR em `/api/orcamento/{id}` (GET/PUT/DELETE/despesa)                       │ **404**, dados do dono intactos ✅                  │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Aporte com `carteiraId` de outro usuário                                     │ **404** ✅                                          │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Injeção SQL no login (`' OR 1=1 --`, `admin'--`)                             │ **401**, sem 500 ✅                                 │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Injeção SQL no ticker                                                        │ **404**, sem 500 ✅                                 │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Swagger em Production                                                        │ **404** (só exposto em Development) ✅              │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ CORS de origem `evil.example`                                                │ sem `Access-Control-Allow-Origin` ✅                │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ CORS de `localhost:5173`                                                     │ permitido ✅                                        │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Cadastro em massa                                                            │ **10×201 → 429 + Retry-After** (após a correção) ✅ │
   ├──────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
   │ Exceções não tratadas no log                                                 │ **0** ✅                                            │
   └──────────────────────────────────────────────────────────────────────────────┴─────────────────────────────────────────────────────┘

   ## 5. Achados que **não** corrigi (e por quê)

   1. **POST /api/usuario/{id}/verificar é anônimo e aceita qualquer id existente.** Qualquer cliente marca uma conta como verificada e enumera ids (200 vs 404). Corrigir exige código/segredo por e-mail — e o
   Cadastro.tsx chama essa rota sem token logo após o cadastro. Fechar isso quebraria o frontend, que você mandou não alterar.
   2. Qualquer usuário autenticado altera o catálogo global e o preço de mercado (POST /api/ativo, POST /api/ativo/{ticker}/simular). Não há papel de administrador; restringir quebraria a tela de Investimentos.
   3. **Sem UseHttpsRedirection/HSTS e sem cabeçalhos de segurança** (só Server: Kestrel). Em API JSON local isso é configuração de deploy; adicionar redirecionamento sem endpoint HTTPS só gera aviso, e CSP
   quebraria o Swagger em dev.
   4. **Swashbuckle.AspNetCore 6.5.0** está defasado (exposição já restrita a Development). Não atualizei: o restore NuGet está quebrado neste shell.
   5. **AllowedHosts: "*"** — restringir depende do domínio de produção.
   6. Nits de coesão (D): UsuarioService()/AtivoService(IGeradorDeVariacao) e AppDbContextEmMemoria são costura de teste dentro do assembly de produção. Não é vulnerabilidade; remover exigiria reescrever vários
    testes.

   ## 6. Por que **nota 10 é aritmeticamente impossível** sem mexer no frontend

   A régua é 0,70 × POO + 0,30 × Frontend. Bloco POO (A–H) pode chegar a 10. O bloco Frontend foi avaliado em 8,0 (I=9, J=7, K=7).

   Mesmo com POO = 10, o teto é 7,0 + 2,4 = 9,4.

   Para 10,0 seria preciso Frontend = 10 — ou seja, alterar I/J/K. Observação relevante: as queixas de J e K na avaliação ("alert", "sem token/sessão", organização) já não existem no código atual, então uma
   reavaliação pode subir esses pontos sem eu tocar em nada. Mas isso não posso garantir.

   ## 7. Validações executadas

   ┌────────────────────────────────────────────────────┬────────────────────────────────────────────────────────────────┐
   │ Gate                                               │ Resultado                                                      │
   ├────────────────────────────────────────────────────┼────────────────────────────────────────────────────────────────┤
   │ `dotnet build Portfy.csproj --no-restore`          │ **0 avisos, 0 erros**                                          │
   ├────────────────────────────────────────────────────┼────────────────────────────────────────────────────────────────┤
   │ Compilação do projeto de testes                    │ limpa                                                          │
   ├────────────────────────────────────────────────────┼────────────────────────────────────────────────────────────────┤
   │ Suíte completa (`dotnet vstest`)                   │ **222/222 aprovados** (181 originais + 41 novos)               │
   ├────────────────────────────────────────────────────┼────────────────────────────────────────────────────────────────┤
   │ Sonda de segurança em Production                   │ todos os controles acima ✅                                    │
   ├────────────────────────────────────────────────────┼────────────────────────────────────────────────────────────────┤
   │ Arquivos temporários / processos / bancos de sonda │ removidos; nenhuma porta ocupada                               │
   ├────────────────────────────────────────────────────┼────────────────────────────────────────────────────────────────┤
   │ `git status`                                       │ 3 arquivos modificados + 4 testes novos; **frontend intocado** │
   └────────────────────────────────────────────────────┴────────────────────────────────────────────────────────────────┘

   Os itens notas/relatorios_prompt/* no git status já existiam antes desta sessão — não são meus.

   Nada foi commitado (o contrato exige autorização explícita).

   Se quiser perseguir o 10,0, o caminho é autorizar as correções de frontend (J/K) — posso listar exatamente o que seria feito antes de executar.