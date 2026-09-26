# Portfy - Sistema Modular de Simulação Financeira

O **Portfy** é um sistema em C# voltado para a gestão de orçamento pessoal e simulação de investimentos em carteira.

---

## Integrantes do Grupo

- Carlos Florenzano
- Lucas Gomes
- Sérgio Lucas Gomes
- Arthur Salonikio
- Karina Braga Louzada
- Miguel Martins

## Log de Alterações e Arquitetura — MVP (23/09/2026)

### 1. Justificativa de Engenharia de Software

Adotou-se a estratégia de **Refatoração Modular Incremental** antes da integração geral no `Dashboard.cs`. A fragmentação em duas fases — (1) padronização/saneamento das entidades core e (2) orquestração dos módulos — isola erros de compilação, elimina vazamentos de estado por membros estáticos e garante que a camada de apresentação interaja exclusivamente com modelos *domain-driven* robustos e imutáveis por padrão.

---

### 2. Saneamento do Core Financeiro e Correções Técnicas

* **`Usuario.cs`**: Encapsulamento de mutadores (`private set`), validação defensiva via construtor e migração para o namespace `GestaoSalarioOrcamento`.
* **`Ativo.cs`**: Isolamento em arquivo próprio e adição de validação de cotações positivas.
* **`AporteSimulado.cs`**: Implementação de construtor e fechamento de acessores do estado.
* **`CarteiraSimulada.cs`**:
  * Adição de mecanismo de sincronização (`lock`) para **Thread Safety** durante movimentações.
  * Correção da regra de negócio do Patrimônio Total (uso do `PrecoAtual` da cotação de mercado em vez do custo histórico `PrecoMedio`).
  * Proteção de coleções usando `IReadOnlyCollection` contra manipulação externa indevida (ex: `.Clear()`).
  * Padronização de todos os tipos monetários para `decimal` (garantia de precisão de centavos).
  * Remoção de acoplamento com o `Console` em conformidade com o **SRP (Princípio da Responsabilidade Única)**.
* **`Program.cs` & `Dashboard.cs`**: Estabelecida a estrutura de injeção de dependência no orquestrador e criado o laço contínuo do menu interativo no terminal.

* ** Modificações feitas em `Dashboard.cs`**
 * Melhoria da navegação: ajustado o fluxo dos menus de Salário/Orçamento e Negociação de Ativos para permitir o retorno ao menu anterior sem voltar diretamente ao menu principal.
 * Cancelamento de operações: adicionada a opção 0 para cancelar operações durante a entrada de dados.
 * Correção da leitura de dados: corrigidos os fluxos que realizavam mais de uma leitura (Console.ReadLine()) para a mesma informação, evitando que o usuário precisasse digitar os dados duas vezes.
 * Validação de entradas: aprimoradas as validações de valores inválidos, quantidades e opções de menu.
 * Mensagens de navegação: adicionadas mensagens informando quando uma operação foi cancelada ou quando ocorreu uma entrada inválida.
 * Testes de navegação: realizadas verificações nos menus de orçamento, aporte e negociação de ativos para garantir o retorno correto ao menu anterior após cada operação.
 * Todas feitas na branch Dashboard-e-relatorio

---

### 3. Padronização de Código (.NET 6+ / C# 10+)

Uso exclusivo de **File-Scoped Namespaces** (`namespace Modulo;`) para eliminação de aninhamento por chaves, melhorando a organização visual e padronizando a leitura em todo o repositório.

---

### 4. Pendências e Próximos Passos

* **Transição para a Web:** Adoção e integração de uma interface moderna no frontend utilizando **TypeScript** e **React**, conectando-se ao core de domínio do .NET por meio de uma Web API RESTful.
