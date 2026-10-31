# Portfy - Sistema Modular de Simulação Financeira

O **Portfy** é um sistema em C# voltado para a gestão de orçamento pessoal e simulação de investimentos em carteira, em transição da sua estrutura inicial para uma **Web API RESTful** (.NET) preparada para integração com um frontend em **React + TypeScript**.

---

## Integrantes do Grupo

- Carlos Florenzano, MA: 06014437
- Lucas Gomes, MA: 06014478
- Sérgio Lucas Gomes, MA: 06015126
- Arthur Salonikio, MA: 06014408
- Karina Braga Louzada, MA: 06013932
- Miguel Martins, MA: 06014120

---

## Log de Alterações e Arquitetura (21/10/2026)

### 1. Justificativa de Engenharia de Software

Adotou-se a **implementação incremental** para as funcionalidades, correções estruturais dentro do software em alguns pontos para a implementação do banco de dados SQLite, e ajustes finos com base em feedbacks presentes em /notas.

---

### 2. Principais Atualizações e Reestruturações com Base em Avaliações Externas
**Lista de Ajustes da AV1 (fechados em 30/10/2026):**
* *Ajustes de Conceitos de Abstração [x]*
* *Polimorfismo [x]*
* *Testes das Regras [x]*
* *Responsabilidades/Coesão [x]*
* *Colaboração entre Objetos [x]*
* *Tratamento de Erros no Frontend [x]*
* *Injeção de Dependências [x]*

---

### 3. Ajustes de 30/10/2026 — fechamento do tópico "O que faltou" da AV1

| Item da AV1 | O que foi feito |
|---|---|
| Nenhuma interface / sem injeção de dependência | 6 contratos em `Services/Contracts/` (`ICarteiraService`, `IUsuarioService`, `IAtivoService`, `IOrcamentoService`, `IAporteService`, `IGeradorDeVariacao`), implementados em `Services/` e registrados no container em `Program.cs`. Nenhum controller tem mais estado `static`. |
| Nenhum teste | Projeto `tests/Portfy.Tests` (xUnit) com 61 testes: invariantes de carteira, posição, orçamento, usuário e ativo; cálculo de variação com dublê de `IGeradorDeVariacao`; controller exercitado com dublê de `ICarteiraService`. |
| Erros no frontend via `alert`/`console.error` | Interceptor de resposta em `src/services/api.ts` traduz ProblemDetails/envelope legado para `ErroRequisicao`; as telas exibem a mensagem em tela (login, cadastro e dashboard). |
| Herança e polimorfismo | Já resolvidos em `4a8be0a` (`Ativo` abstrato + 5 subclasses); o catálogo do `AtivoService` usa as subclasses concretas. |
| `UI/Dashboard.cs` órfão | Já removido em `cbe4c05`. |

Gates executados em 30/10/2026: `dotnet build Portfy.csproj` (0 erros, 0 warnings), `dotnet test tests/Portfy.Tests` (61/61), `cd frontend && npm run build && npm run lint` (ambos verdes), `grep -rn "alert(" frontend/src` e `grep -rn "private static" Controllers Services` (vazios).
