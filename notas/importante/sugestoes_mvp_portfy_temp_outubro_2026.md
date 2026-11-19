# Sugestões para o MVP do Portfy

---

## 1. Visão Geral do MVP

O objetivo do **Portfy** é unificar a gestão de orçamento, simulação de aportes e alocação percentual baseada em gráficos de pizza em uma única ferramenta amigável para iniciantes, sem deixar de atender investidores avançados. 

Para a construção do **MVP com SQLite**, o foco deve estar na estabilidade do núcleo da aplicação, garantindo que o usuário consiga simular cenários financeiros completos (investimentos + despesas) de forma isolada através de múltiplos perfis/simulações.

---

## 2. Modelagem Básica do Banco de Dados (SQLite)

Para suportar as funcionalidades propostas no repositório, sugere-se a seguinte estrutura relacional simplificada para o SQLite:

* **`simulations`**: Armazena as múltiplas simulações/usuários.
  * `id` (INTEGER, PK)
  * `name` (TEXT) - Ex: "Carteira Agressiva 2026", "Foco em Dividendos"
  * `created_at` (DATETIME)
* **`assets`**: Cadastro base ou cache de ativos pesquisados (ações, FIIs, ETFs globais).
  * `id` (INTEGER, PK)
  * `ticker` (TEXT) - Ex: `AAPL`, `PETR4`, `VT`
  * `name` (TEXT)
  * `country` (TEXT) - Ex: `BR`, `US`
  * `currency` (TEXT) - Ex: `BRL`, `USD`
* **`portfolio_allocations`**: Define a meta percentual da pizza para cada ativo/classe dentro de uma simulação.
  * `id` (INTEGER, PK)
  * `simulation_id` (INTEGER, FK)
  * `asset_id` (INTEGER, FK)
  * `target_percentage` (REAL) - Ex: 25.0 (%)
* **`expenses`**: Orçamentos dedicados a pagamento de contas e despesas vinculados à simulação.
  * `id` (INTEGER, PK)
  * `simulation_id` (INTEGER, FK)
  * `category` (TEXT) - Ex: "Moradia", "Lazer", "Contas Fixas"
  * `allocated_amount` (REAL) - Valor ou porcentagem destinada

---

## 3. Sugestões de Implementação por Funcionalidade

### 3.1. Simulação de Aportes em Investimentos Globais
* **Cálculo de Rebalanceamento Automático:** Ao inserir um valor total de aporte (ex: R$ 5.000,00 ou US$ 1.000,00), o sistema deve cruzar o patrimônio atual com a meta da "pizza" e indicar exatamente **quanto comprar de cada ativo** para aproximar a carteira do ideal.
* **Conversão de Moedas Simplificada (MVP):** Como os ativos são de diferentes países, implemente uma tabela de câmbio básica (com atualização manual ou via API gratuita simples) para que o gráfico de pizza exiba tudo unificado na moeda base da simulação.

### 3.2. Pesquisa de Ativos de Diferentes Lugares
* **Busca por Ticker/Nome:** Uma barra de pesquisa rápida que filtre ativos locais (B3) e internacionais (como EUA).
* **Lista de Favoritos/Pré-cadastrados:** Para garantir que o MVP funcione mesmo sem conexão com a internet ou caso a API externa falhe, inclua um seed inicial no SQLite com os principais índices globais (ex: S&P 500, Tesouro Direto, principais ações da B3 e ETFs globais).

### 3.3. Divisão de Gastos e Alocação da Carteira
* **Visão Consolidada (Gráfico de Pizza Múltiplo ou em Abas):** 
  * *Visão Macro:* Patrimônio total dividido entre **Investimentos** vs. **Reservas/Despesas**.
  * *Visão Micro:* Detalhamento da pizza de investimentos e detalhamento da pizza de orçamento mensal.
* **Simulador "E se?":** Permitir alterar os percentuais da pizza em tempo real e visualizar o impacto visual imediato antes de salvar as alterações no SQLite.

### 3.4. Orçamentos Dedicados a Contas e Despesas
* **Categorização Fixa vs. Variável:** Permitir que o usuário defina despesas recorrentes (aluguel, contas de consumo) para que o simulador calcule quanto sobra do rendimento mensal para novos aportes em investimentos.
* **Indicador de Saúde Financeira:** Um pequeno card informando a taxa de poupança atual (ex: *"Você está investindo 30% do seu aporte total simulado"*).

### 3.5. Múltiplas Simulações e Testes
* **Seletor de Perfil na Home:** Um menu suspenso (dropdown) na interface principal permitindo alternar instantaneamente entre diferentes simulações salvas no SQLite.
* **Clonagem de Simulações:** Um botão de "Duplicar Simulação", ideal para testar estratégias agressivas versus conservadoras a partir de uma base já existente sem alterar o cenário original.

---

## 4. Próximos Passos Recomendados para o Desenvolvimento

1. **Definição da Stack:** Garantir que o backend e o frontend estejam comunicando perfeitamente com o SQLite (ex: Python com SQLite/SQLAlchemy ou Node.js com SQLite, dependendo da escolha atual do repositório).
2. **Interface Limpa (UI/UX):** Priorizar bibliotecas de gráficos leves e responsivas (como *Chart.js* ou *Recharts*) para renderizar a pizza de alocação de forma fluida.
3. **Testes do MVP:** Validar se o fluxo completo (Criar Simulação -> Definir Metas em % -> Simular Aporte -> Distribuir Despesas) funciona sem erros de arredondamento nas porcentagens (garantindo que a soma feche em 100%).
