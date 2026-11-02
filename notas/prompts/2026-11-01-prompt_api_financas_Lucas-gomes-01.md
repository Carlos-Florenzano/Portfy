# Especificação de Tarefa: Integração de APIs Financeiras no Portfy



## 1. Contexto e Localização do Projeto

* Caminho do Repositório: C:\Users\Lucas\Desktop\projetos\portfy

* Instrução Inicial: A IA deve ler e analisar todo o repositório localizado no caminho acima para compreender a arquitetura atual, os modelos de dados, a stack tecnológica e a estrutura de serviços existente antes de iniciar a implementação.



---



## 2. Objetivo Principal

Implementar e integrar três fontes de dados financeiros e econômicos (FMP, BRAPI e Banco Central - SGS) de forma complementar. A estratégia deve garantir que os pontos fracos, limites de requisição ou lacunas de cobertura de uma API sejam supridos pelas outras, criando um ecossistema robusto e resiliente para o projeto Portfy.



---



## 3. APIs a Serem Integradas



### 3.1. FMP (Financial Modeling Prep - Plano Basic)

* Escopo: Dados fundamentais globais, balanços, demonstrações de resultados, métricas financeiras, cotações internacionais e dados históricos mais profundos.

* Atenção: Considerar as limitações do plano Basic (como limites de requisições diárias/minuto e restrições de endpoints específicos).



### 3.2. BRAPI

* Escopo: Dados de ativos brasileiros (Ações da B3, FIIs, criptomoedas) em tempo real ou atrasado, cotações e informações de mercado nacional com alta agilidade.

* Atenção: Utilizar para otimizar a cobertura do mercado local brasileiro onde o FMP possa ser limitado ou custoso.



### 3.3. Banco Central do Brasil (SGS - Sistema Gerenciador de Séries Temporais)

* Escopo: Séries temporais econômicas oficiais do Brasil (IPCA, Selic, CDI, câmbio, inflação, etc.).

* Atenção: Fonte primária e oficial para indicadores macroeconômicos brasileiros, garantindo precisão em taxas de juros e índices oficiais.



---



## 4. Diretrizes de Implementação e Estratégia de Complementaridade

1. Fallback e Resiliência: Se uma API falhar ou atingir seu limite de taxa (rate limit), o sistema deve ter mecanismos de rotação ou fallback inteligente quando aplicável.

2. Divisão de Responsabilidades:

   * Indicadores macroeconômicos e taxas oficiais -> Banco Central (SGS).

   * Ativos da B3 e dados rápidos do mercado nacional -> BRAPI.

   * Ativos globais, dados fundamentalistas profundos e análises financeiras -> FMP.

3. Padronização: Unificar os retornos das diferentes APIs em contratos/interfaces padronizados dentro do Portfy para que o restante da aplicação consuma os dados de forma transparente, independentemente da origem.