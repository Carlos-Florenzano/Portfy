 # Botão de mostrar/ocultar senha — Login e Cadastro

   ## Comportamento implementado

   ┌──────────────┬─────────────────────────┬───────────────────────────────────────────────────────────────┐
   │ Tela         │ Campos                  │ Botões                                                        │
   ├──────────────┼─────────────────────────┼───────────────────────────────────────────────────────────────┤
   │ **Login**    │ Senha                   │ 1 botão (olho)                                                │
   ├──────────────┼─────────────────────────┼───────────────────────────────────────────────────────────────┤
   │ **Cadastro** │ Senha + Confirmar senha │ **1 botão em cada campo**, independentes (como você escolheu) │
   └──────────────┴─────────────────────────┴───────────────────────────────────────────────────────────────┘

   - Clicou no olho → a senha aparece em texto; clicou de novo → volta a ficar oculta (type alterna entre password e text).
   - No cadastro, revelar "Senha" não revela "Confirmar senha".
   - O botão é type="button", então não envia o formulário ao ser clicado.
   - O valor digitado é preservado ao alternar (só muda o tipo do input).

   ## Acessibilidade

   - aria-label dinâmico: "Mostrar senha" / "Ocultar senha".
   - aria-pressed reflete o estado (leitor de tela anuncia como botão de alternância).
   - title com a mesma dica, para tooltip no mouse.
   - Foco visível: contorno azul #2563eb via :focus-visible.
   - Ícone com aria-hidden="true" (o nome acessível vem do aria-label, sem leitura duplicada).

   ## Arquivos alterados

   ┌─────────────────────────────┬────────────────────────────────────────────────────────────────────┐
   │ Arquivo                     │ Alteração                                                          │
   ├─────────────────────────────┼────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/Login.tsx`    │ `senhaVisivel` + wrapper `.campo-senha` + botão com `Eye`/`EyeOff` │
   ├─────────────────────────────┼────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/Cadastro.tsx` │ `senhaVisivel` e `confirmarSenhaVisivel` (independentes) + botões  │
   ├─────────────────────────────┼────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/Login.css`    │ `.campo-senha`, `.botao-senha` (+ hover e foco)                    │
   ├─────────────────────────────┼────────────────────────────────────────────────────────────────────┤
   │ `frontend/src/Cadastro.css` │ idem, nas cores do arquivo (`#2563eb`)                             │
   └─────────────────────────────┴────────────────────────────────────────────────────────────────────┘

   O CSS reaproveita as variáveis visuais existentes: fundo transparente (herda o tema escuro), border-radius: 6px, hover com rgba(255,255,255,0.14) e o azul #2563eb no foco. O input ganhou padding-right: 48px
   para o texto não ficar embaixo do ícone.

   ## Validações

   ┌─────────────────┬──────────────────────────────────────────────────────┐
   │ Gate            │ Resultado                                            │
   ├─────────────────┼──────────────────────────────────────────────────────┤
   │ `npx tsc -b`    │ exit 0                                               │
   ├─────────────────┼──────────────────────────────────────────────────────┤
   │ `npm run lint`  │ exit 0, sem saída                                    │
   ├─────────────────┼──────────────────────────────────────────────────────┤
   │ `npm run build` │ sucesso — 2.711 módulos, CSS 23,24 kB / JS 782,85 kB │
   └─────────────────┴──────────────────────────────────────────────────────┘

   O aviso de chunk > 500 kB é o já existente (ECharts + React), não é erro.