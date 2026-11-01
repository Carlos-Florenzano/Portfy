# Avaliação — Portfy

**Nota final: 6,92**  ·  Bloco POO (70%): **6,45**  ·  Bloco Frontend (30%): **8,00**

Régua: `correcao/REGUA.md` · Base: livro *Design e Programação Orientados a Objetos*, caps. 1–9.

---

## Notas por dimensão

| Bloco POO | Nota | | Bloco Frontend | Nota |
|---|---|---|---|---|
| A — Modelagem do domínio (10%) | 7,0 | | I — CRUD funcional (50%) | 9,0 |
| B — Estado/comportamento/construtor (15%) | 9,0 | | J — Integração front↔back (25%) | 7,0 |
| C — Encapsulamento/invariantes (20%) | 9,0 | | K — Organização do frontend (25%) | 7,0 |
| D — Responsabilidades/coesão (20%) | 5,5 | | | |
| E — Colaboração entre objetos (15%) | 8,0 | | | |
| F — Abstração (10%) | 2,0 | | | |
| G — Polimorfismo (5%) | 2,0 | | | |
| H — Testes das regras (5%) | 0,0 | | | |

## O que o grupo fez bem
- **A melhor aplicação do cap. 3 e do cap. 4 do conjunto.** Estado sempre por propriedade e **zero** `{ get; set; }` nos models — só `private set` ou `get;`.
- **Coleções protegidas do jeito certo:** listas privadas expostas como `IReadOnlyCollection` (`_posicoes.AsReadOnly()`), exatamente como o cap. 6 pede.
- **Invariantes fortes e verificadas nas operações:** saldo insuficiente, quantidade de compra/venda maior que zero, preço médio positivo, remoção maior que a posição disponível.
- **Construtores que garantem o estado inicial** (`CarteiraSimulada(decimal saldoInicial)` recusa saldo negativo; `Posicao` valida quantidade e preço médio).
- **Preocupação real com integridade:** uso de `lock` e comentário explicando o objetivo (concorrência em dados financeiros).
- **Colaboração bem modelada:** `CarteiraSimulada` agrega `Posicao` (criada e removida pelo próprio dono quando a quantidade zera) e compõe `AporteSimulado`; `Posicao` referencia `Ativo`, que tem vida independente — composição × associação corretas.
- **Frontend com CRUD completo e rotulado:** `UsuarioService.ts` tem `cadastrar` (create), `obterPorId`/`obterTodos` (read), `atualizar` (update) e `excluir` (delete), e as telas consomem essas operações.

## O que faltou
- **Nenhuma interface e nenhuma herança** em todo o backend — a abstração (cap. 8) está simplesmente ausente.
- **Sem injeção de dependência:** `Program.cs` só configura MVC/Swagger/CORS; o estado é global (`private static readonly CarteiraSimulada _carteira`).
- **Código órfão que mistura responsabilidades:** `UI/Dashboard.cs` tem **701 linhas** de interface de console, com menus, fluxos e estado de domínio (listas de `Orcamento`/`Ativo`), e **não está ligado ao `Program.cs`**. Um projeto Web API não deveria carregar uma aplicação de console paralela e abandonada.
- **Nenhum teste**, apesar de as invariantes serem exatamente o tipo de regra que se testa (cap. 4 e 9).
- **Erros no frontend tratados com `alert`/`console.error`**, sem token/sessão.

## O que faria a nota subir
1. **Remover ou converter o `Dashboard.cs`.** Se a ideia era uma UI de console, ela não pertence a este projeto; se era um protótipo, apague. Sobe D de 5,5 para ~7 só por eliminar a mistura e o código morto.
2. **Introduzir DI e uma interface para a carteira** (`ICarteiraRepository` ou `ICarteiraSimulada`) e injetar. Sobe F de 2,0 para ~8.
3. **Escrever testes das invariantes** que já existem: compra sem saldo, venda sem posição, aporte nulo, redução maior que a quantidade, preço médio após múltiplas compras. Sobe H de 0 para ~9 em pouco esforço, porque a regra já está isolada e fácil de testar.
4. **Tratar erros no frontend** com mensagem em tela e um interceptor no `api.ts`, em vez de `alert`.

## Observação
Este grupo tem o **modelo de domínio mais próximo do que o livro ensina** nos caps. 2 a 4. A nota não é maior porque o cap. 5 (coesão), o cap. 8 (abstração) e o cap. 9 (polimorfismo) ficaram sem resposta — e porque faltou verificação por testes.
