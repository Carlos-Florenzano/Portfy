# Portfy - Sistema Modular de Simulação Financeira

O **Portfy** é um sistema em C# voltado para a gestão de orçamento pessoal e simulação de investimentos em carteira. O projeto adota princípios modernos de engenharia de software (.NET 6+ / C# 10+), focando em **SOLID**, **mutabilidade defensiva**, **precisão numérica** e **Thread Safety**.

---

##  Log de Alterações e Arquitetura — MVP (22/09/2026)

### 1. Justificativa de Engenharia de Software
Adotou-se a estratégia de **Refatoração Modular Incremental** antes da integração geral no `Dashboard.cs`. A fragmentação em duas fases — (1) padronização/saneamento das entidades core e (2) orquestração dos módulos — isola erros de compilação, elimina vazamentos de estado por membros estáticos e garante que a camada de apresentação interaja exclusivamente com modelos *domain-driven* robustos e imutáveis por padrão.

---

### 2. Saneamento do Core Financeiro e Correções Técnicas

* **`Usuario.cs`**: Encapsulamento de mutadores (`private set`), validação defensiva via construtor e organização no namespace `Portfy.Dominio.Financas`.
* **`Ativo.cs`**: Isolamento em arquivo próprio e adição de validação de cotações positivas.
* **`AporteSimulado.cs`**: Implementação de construtor e fechamento de acessores do estado.
* **`CarteiraSimulada.cs`**:
  * Adição de mecanismo de sincronização (`lock`) para **Thread Safety** durante movimentações.
  * Correção da regra de negócio do Patrimônio Total (uso do `PrecoAtual` da cotação de mercado em vez do custo histórico `PrecoMedio`).
  * Proteção de coleções usando `IReadOnlyCollection` contra manipulação externa indevida (ex: `.Clear()`).
  * Padronização de todos os tipos monetários para `decimal` (garantia de precisão de centavos).
  * Remoção de acoplamento com o `Console` em conformidade com o **SRP (Princípio da Responsabilidade Única)**.
* **`Program.cs` & `Dashboard.cs`**: Estabelecida a estrutura de injeção de dependência no orquestrador e criado o laço contínuo do menu interativo no terminal.

---

### 3. Padronização de Código (.NET 6+ / C# 10+)
Uso exclusivo de **File-Scoped Namespaces** (`namespace Modulo;`) para eliminação de aninhamento por chaves, melhorando a organização visual e padronizando a leitura em todo o repositório.

---

### 4. Padrão de Nomenclatura
- Todos os namespaces começam com `Portfy` e usam segmentos PascalCase que representam a camada e o domínio.
- `Portfy.Dominio.Financas` contém `Usuario` e `Orcamento`.
- `Portfy.Dominio.Investimentos` contém `AporteSimulado`, `Ativo`, `CarteiraSimulada`, `Posicao` e `TipoAtivo`.
- `Portfy.Apresentacao` contém `Dashboard` e `RelatorioFinanceiro`; `Program` permanece na raiz `Portfy`.
- Classes, enums, métodos e propriedades usam PascalCase; campos privados usam `_camelCase`.
- Cada arquivo de tipo público mantém o nome do tipo principal. Os nomes de domínio permanecem em português.

---

### 5. Pedidos de Investimento
No menu principal, a opção **8. Gerenciar Pedidos de Investimento** permite criar pedidos de compra/venda e consultar o histórico da sessão. Cada pedido recebe código e data/hora, registra ativo, quantidade, preço unitário, valor total e status. A execução é confirmada pelo usuário e sincronizada imediatamente com `CarteiraSimulada`; tentativas rejeitadas também ficam no histórico com o motivo. Os registros são mantidos em memória e são descartados ao encerrar o aplicativo.

### 6. Pendências e Próximos Passos
- **Integração do Módulo `Orcamento.cs`**: Consolidar seu uso junto ao módulo `Portfy.Dominio.Financas`.
- **Customização do Dashboard**: Adaptação e polimento visual da interface pelo responsável do módulo de apresentação.
- **Transição de Arquitetura**: Finalização do ciclo de testes do MVP em Console CLI para futura migração da camada de domínio para uma estrutura **Web API**.
