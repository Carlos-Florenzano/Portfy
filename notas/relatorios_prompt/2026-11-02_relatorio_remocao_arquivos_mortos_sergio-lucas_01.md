 Item 1 executado + varredura de código morto

   ## Arquivos removidos (9)

   ┌──────────────────────────────────┬──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
   │ Arquivo                          │ Evidência de que estava morto                                                                                                                                        │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Models/Posicao.cs`              │ **0 bytes de código efetivo** — 100% dentro de `/* ... */`. Gerava zero tipos.                                                                                       │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Services/RelatorioFinanceiro.cs │ Classe de relatório **para console** (`Console.Clear`/`WriteLine`), **0 referências** em todo o código. Substituída por `RelatorioService` + `IRelatorioService` +   │
   │ `                                │ `Dtos/Relatorio/*`.                                                                                                                                                  │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/components/Ativos. │ **0 imports**. Substituído por `pages/Investimentos.tsx`.                                                                                                            │
   │ tsx`                             │                                                                                                                                                                      │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/App.css`           │ **0 imports**. CSS do template Vite (`.counter`, `.hero`, `.base`, `.framework`, `.vite`), usando `var(--accent)` que não existe mais no projeto.                    │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/assets/hero.png`   │ **0 referências** (nem `url()` em CSS).                                                                                                                              │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/assets/react.svg`  │ **0 referências**. Logo do React do template.                                                                                                                        │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/assets/vite.svg`   │ **0 referências**. Logo do Vite do template.                                                                                                                         │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `frontend/public/icons.svg`      │ **0 referências**. Sprite de ícones sociais do template.                                                                                                             │
   ├──────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `frontend/public/favicon.svg`    │ Referenciado **apenas dentro de comentário HTML** (o próprio comentário dizia "o favicon do Vite"). O ativo é `/Portfy.svg`.                                         │
   └──────────────────────────────────┴──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

   Também removi a pasta vazia frontend/src/assets/ e o comentário obsoleto em frontend/index.html que apontava para o favicon.svg deletado (o <link> ativo para /Portfy.svg foi preservado).

   ---

   ## Metodologia (para você poder auditar)

   Backend — 157 arquivos, 167 tipos declarados, com comentários removidos antes da contagem (para não contar referências dentro de blocos comentados, como era o caso de Posicao.cs). Separei os falsos positivos
    que são ligados por reflexão:

   ┌──────────────────┬─────────┬──────────────────────────────────────────────┐
   │ Categoria        │ Quantos │ Mecanismo                                    │
   ├──────────────────┼─────────┼──────────────────────────────────────────────┤
   │ Controllers      │ 7       │ `[ApiController]` + roteamento por convenção │
   ├──────────────────┼─────────┼──────────────────────────────────────────────┤
   │ Configurações EF │ 11      │ `ApplyConfigurationsFromAssembly`            │
   ├──────────────────┼─────────┼──────────────────────────────────────────────┤
   │ Validators       │ 7       │ `AddValidatorsFromAssembly`                  │
   ├──────────────────┼─────────┼──────────────────────────────────────────────┤
   │ Testes xUnit     │ 22      │ descoberta por reflexão                      │
   └──────────────────┴─────────┴──────────────────────────────────────────────┘

   Frontend — resolvi os imports de verdade (specifiers relativos, sem extensão, index.*, @import de CSS, url(), href do index.html) e depois fiz checagem de **alcançabilidade a partir de main.tsx**.

   ---

   ## Falsos candidatos que **mantive** (e por quê)

   - **Services/Integracoes/RegistroDeIntegracoes.cs — o nome da classe não aparece em lugar nenhum, mas os métodos de extensão** sim:
     - Program.cs:145 → builder.Services.AdicionarIntegracoesDeMercado(...)
     - Program.cs:189 → app.Services.AvisarFontesDeMercadoNaoConfiguradas()

     É um falso positivo clássico de extension methods. Vivo.
   - **Data/AppDbContextEmMemoria.cs** — usado pelos construtores sem parâmetro de AtivoService/UsuarioService (caminho dos testes). Vivo.
   - **Dtos/Carteira/PosicaoResponse.cs, PosicaoConfiguration.cs, PosicaoTests.cs, MigracaoPosseTests.cs** — todos ligados à classe Posicao ativa em Models/CarteiraSimulada.cs. Vivos (só o arquivo comentado
   morreu).
   - **docs/, notas/, management/** — documentação intencional, não toquei.

   ---

   ## Gates executados após as remoções

   ┌──────────────────────────────────────────────────┬─────────────────────────────────────────────────────────────────────────────────┐
   │ Gate                                             │ Resultado                                                                       │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ `dotnet build --no-restore`                      │ **0 avisos, 0 erros**                                                           │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ `dotnet test --no-restore`                       │ **170/170 aprovados** (mesmo número de antes)                                   │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ `npx tsc -b`                                     │ limpo                                                                           │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ `npm run lint`                                   │ limpo                                                                           │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ `npm run build`                                  │ sucesso — CSS caiu de 23,68 kB → **22,02 kB** (reflexo da remoção do `App.css`) │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ `dotnet ef migrations has-pending-model-changes` │ "No changes…"                                                                   │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ Referências residuais aos 9 arquivos             │ **0**                                                                           │
   ├──────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────┤
   │ Alcançabilidade do frontend                      │ **32/32 arquivos** alcançáveis de `main.tsx` (0 órfãos)                         │
   └──────────────────────────────────────────────────┴─────────────────────────────────────────────────────────────────────────────────┘

   Reexecutei a análise depois das remoções: o único "candidato" restante é RegistroDeIntegracoes, que provei ser vivo.

   ---

   ## Dois pontos que você precisa saber

   1. **O docs/arch.md ficou desatualizado por causa desta remoção.** Ele ainda trata Services/RelatorioFinanceiro.cs como dívida aberta — o que agora é falso:
      - linha 49: "parcial: hoje imprime em console"
      - linha 114: "Resíduo de console no backend"
      - linha 243 e 655: "Services/ escrever em Console" / "❌ RelatorioFinanceiro.cs (P2-a)"
      - linha 737: "P2-a | ... escreve em console"

      Na prática, P2-a está fechado (o RelatorioService já retorna DTO). Não editei a documentação porque você não pediu — quer que eu atualize essas linhas?

   2. Nada foi commitado. Os 9 arquivos estão versionados no git, então tudo é recuperável com git checkout -- <arquivo>. O git status mostra apenas: 9 deleções + 1 edição (frontend/index.html).