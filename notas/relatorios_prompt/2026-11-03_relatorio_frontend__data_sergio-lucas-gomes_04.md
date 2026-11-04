# Relatórios — período agora aceita data digitada

   ## Alteração

   Apenas frontend/src/pages/Relatorios.tsx.

   Os campos De e Até deixaram de ser type="date" puros e passaram a ser um campo de texto com máscara dd/mm/aaaa, mantendo o calendário nativo acessível pelo ícone ao lado.

   ### Comportamento

   ┌──────────────────────────────────────────────┬───────────────────────────────────────────────────────────────────────────────────────┐
   │ Ação                                         │ Resultado                                                                             │
   ├──────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────┤
   │ Digitar `02112026`                           │ vira `02/11/2026` automaticamente                                                     │
   ├──────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────┤
   │ Digitar `02/11/2026`                         │ funciona normalmente (as barras são reorganizadas pela máscara)                       │
   ├──────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────┤
   │ Clicar no ícone de calendário                │ abre o seletor nativo e preenche no mesmo formato                                     │
   ├──────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────┤
   │ Deixar vazio                                 │ período sem filtro (comportamento anterior)                                           │
   ├──────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────┤
   │ Data incompleta/inválida (ex.: `31/02/2026`) │ mostra “Data inicial/final inválida. Use o formato dd/mm/aaaa.” e **não** chama a API │
   ├──────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────┤
   │ De > Até                                     │ mostra “A data inicial não pode ser posterior à data final.”                          │
   └──────────────────────────────────────────────┴───────────────────────────────────────────────────────────────────────────────────────┘

   A conversão para o contrato da API (DateOnly? em ISO yyyy-MM-dd) acontece só no momento de gerar/exportar, então GET /api/relatorio e GET /api/relatorio/csv continuam recebendo o mesmo formato de antes.

   ### Layout

   - Mesmas classes classesDeRotulo/classesDeEntrada, mesmo min-w-40 flex-1 e mesmo flex-wrap — nada de tema, cor, tipografia ou espaçamento foi alterado.
   - O input ganhou pr-11 apenas para o ícone CalendarDays (lucide) não cobrir o texto.
   - Botão do calendário com aria-label e foco visível.

   ## Validações

   ┌─────────────────┬──────────────────────────────────────┐
   │ Gate            │ Resultado                            │
   ├─────────────────┼──────────────────────────────────────┤
   │ `npx tsc -b`    │ exit 0                               │
   ├─────────────────┼──────────────────────────────────────┤
   │ `npm run lint`  │ exit 0                               │
   ├─────────────────┼──────────────────────────────────────┤
   │ `npm run build` │ sucesso (780,97 kB / 252,86 kB gzip) │
   └─────────────────┴──────────────────────────────────────┘

   Nenhum commit foi feito. Se o Vite estiver rodando em modo dev, a tela já recarrega; caso use build, o dist/ foi regenerado.