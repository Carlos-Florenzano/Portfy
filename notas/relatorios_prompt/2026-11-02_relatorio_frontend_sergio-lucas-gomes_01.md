 Relatório final — Prompt 02 (frontend/backend Portfy)

   ## Situação encontrada

   O prompt 02 já tinha a maior parte do trabalho executada na árvore de trabalho (não commitada), mas a entrega estava quebrada: o frontend não compilava. src/components/formatadores.ts foi criado para isolar
   funções utilitárias, mas os 7 consumidores continuavam importando formatarMoeda, formatarData, formatarPercentual, classesDeEntrada e classesDeRotulo de ui.tsx — que só exporta componentes. Resultado: 23
   erros TS2305.

   ## O que foi corrigido nesta sessão

   ┌──────┬──────────────────────────────┬──────────────────────────────────────────────────────────┬─────────────────┐
   │ #    │ Arquivo                      │ Correção                                                 │ Motivo          │
   ├──────┼──────────────────────────────┼──────────────────────────────────────────────────────────┼─────────────────┤
   │ 1    │ `GraficoDeOrcamentos.tsx`    │ `formatarMoeda` → `./formatadores`                       │ import quebrado │
   ├──────┼──────────────────────────────┼──────────────────────────────────────────────────────────┼─────────────────┤
   │ 2    │ `PerfilDoUsuario.tsx`        │ helpers → `./formatadores` (componentes ficam em `./ui`) │ import quebrado │
   ├──────┼──────────────────────────────┼──────────────────────────────────────────────────────────┼─────────────────┤
   │ 3    │ `pages/Painel.tsx`           │ idem                                                     │ import quebrado │
   ├──────┼──────────────────────────────┼──────────────────────────────────────────────────────────┼─────────────────┤
   │ 4    │ `pages/SalarioOrcamento.tsx` │ idem                                                     │ import quebrado │
   ├──────┼──────────────────────────────┼──────────────────────────────────────────────────────────┼─────────────────┤
   │ 5    │ `pages/Investimentos.tsx`    │ idem                                                     │ import quebrado │
   ├──────┼──────────────────────────────┼──────────────────────────────────────────────────────────┼─────────────────┤
   │ 6    │ `pages/Aportes.tsx`          │ idem                                                     │ import quebrado │
   ├──────┼──────────────────────────────┼──────────────────────────────────────────────────────────┼─────────────────┤
   │ 7    │ `pages/Relatorios.tsx`       │ idem                                                     │ import quebrado │
   └──────┴──────────────────────────────┴──────────────────────────────────────────────────────────┴─────────────────┘

   Depois disso, o ESLint (gate do repositório) apontou 3 erros reais, todos corrigidos na raiz:

   ┌─────────────────────────┬─────────────────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────────────────────────────────────────────────────────┐
   │ Arquivo                 │ Erro                                                                │ Correção                                                                                                │
   ├─────────────────────────┼─────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `GraficoDeOrcamentos.ts │ `rules-of-hooks`: `use` do ECharts confundido com o hook `use` do   │ alias explícito `use as registrarModulosDoEcharts` — elimina a ambiguidade, não silencia a regra        │
   │ x`                      │ React                                                               │                                                                                                         │
   ├─────────────────────────┼─────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `Dashboard.tsx`         │ `set-state-in-effect`: `void carregarUsuario()` chamado direto no   │ padrão de gatilho (`tentativa`) já usado em `IndicadoresDeMercado`; o efeito passa a ser o único dono   │
   │                         │ efeito                                                              │ da carga                                                                                                │
   ├─────────────────────────┼─────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
   │ `SalarioOrcamento.tsx`  │ `set-state-in-effect`: efeito sincronizando estado local com prop   │ estado único `edicaoDoSalario` (`null` = espelhar a renda do usuário); efeito removido                  │
   └─────────────────────────┴─────────────────────────────────────────────────────────────────────┴─────────────────────────────────────────────────────────────────────────────────────────────────────────┘

   ## Verificação objetiva dos requisitos do prompt

   ┌─────────────────────────────────────────────────┬─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┬───────────────┐
   │ Requisito                                       │ Como foi verificado                                                                                                     │ Resultado     │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Espaçamento entre **Sair** e **Excluir Conta**  │ `BarraLateral.tsx`: `flex flex-col gap-6` (24 px) + `border-t pt-5`                                                     │ ✅            │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Ícones `lucide-react` no lugar de emojis/raster │ varredura programática de emojis em todo `src/` (0 ocorrências) + nenhuma imagem referenciada em botão/menu             │ ✅            │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Cores em sintonia com os mockups                │ extração de cores dominantes dos 5 PNGs via `System.Drawing`                                                            │ ✅ ver abaixo │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Salário e Orçamento funcional                   │ `PUT /usuario/{id}/salario` → 200; `POST/PUT/DELETE /orcamento`; `POST /orcamento/{id}/despesa`; `GET /orcamento/todos` │ ✅            │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Investimentos funcional                         │ `POST /ativo` → 201; `GET /ativo` → 200; `POST /carteira/compras` → 200                                                 │ ✅            │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Aportes funcional                               │ `POST /aporte` → 200; `GET /aporte?inicio&fim` → 200, com total/quantidade                                              │ ✅            │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Relatórios funcional                            │ `GET /relatorio` → 200; `GET /relatorio/csv` → 200 `text/csv` (28 linhas, BOM UTF‑8)                                    │ ✅            │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Gráfico ECharts donut com tipagem               │ `ComposeOption<PieSeriesOption \                                                                                        │ Tooltip \     │
   ├─────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┼───────────────┤
   │ Empty state no lugar do placeholder             │ texto "serão exibidos aqui…" inexistente no código; `EstadoVazio` com ícone Lucide e CTA                                │ ✅            │
   └─────────────────────────────────────────────────┴─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┴───────────────┘

   Paleta comprovada: acentos dos mockups #2563eb (Dashboard), #60a5fa (Relatórios), #fbbf24 (Salário) e texto #ededed são exatamente os tokens de index.css. Base escura #0a0a0a/#121212 vs #000000/#1b1b1b dos
   mockups — harmonizada.

   ## Gates executados

   ┌────────────────────────┬──────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────────────────────────────┐
   │ Gate                   │ Comando                                          │ Resultado                                                                            │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Build backend          │ `dotnet build --no-restore`                      │ êxito, 0 avisos, 0 erros                                                             │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Testes backend         │ `dotnet test --no-restore`                       │ **170 aprovados**, 0 falhas                                                          │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Migrações              │ `dotnet ef migrations has-pending-model-changes` │ "No changes… since the last migration"                                               │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Typecheck frontend     │ `npx tsc -b`                                     │ exit 0                                                                               │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Lint frontend          │ `npm run lint`                                   │ exit 0, sem saída                                                                    │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Build frontend         │ `npm run build`                                  │ êxito (778 kB / 252 kB gzip)                                                         │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ E2E 4 módulos          │ banco descartável `portfy-e2e.db`                │ todos os fluxos 201/200 com payload correto                                          │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Segurança              │ endpoints protegidos sem token                   │ `401` em `/relatorio`, `/aporte`, `/orcamento/todos`; `/ativo` público `200`         │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Isolamento por usuário │ 2 usuários no E2E                                │ carteira/orçamento por usuário (usuário 2 viu saldo 10.000, não 12.397 do usuário 1) │
   ├────────────────────────┼──────────────────────────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────┤
   │ Smoke na base real     │ `dotnet run` + `portfy.db`                       │ `/api/ativo` 200, protegidos 401, **0 exceções** no log                              │
   └────────────────────────┴──────────────────────────────────────────────────┴──────────────────────────────────────────────────────────────────────────────────────┘

   ## Limitações e observações honestas

   1. Operações síncronas no backend (herdado): o contrato dos serviços e os 170 testes são síncronos. Migrar para async exigiria alterar controllers, DTOs e testes em conjunto — não foi feito aqui.
   2. **Login.tsx / Cadastro.tsx não usam lucide-react nem o tema Tailwind.** Não contêm emojis nem imagens rasterizadas em botões/menus (que é o que o prompt pede), portanto foram preservados. Padronizá-los
   seria trabalho adjacente não solicitado.
   3. **Ativos.tsx e assets/hero.png são órfãos** (não importados por ninguém). Não removi: fora do escopo do prompt.
   4. **npm run build emite aviso de chunk > 500 kB** (ECharts + React). É aviso do Vite, não erro; code-splitting não foi solicitado.
   5. Nenhum commit foi feito. O contrato exige autorização explícita para operações de git. A árvore contém apenas as alterações do prompt 02 (mais as 7 correções acima); os artefatos temporários que criei.