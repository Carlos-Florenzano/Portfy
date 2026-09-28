# Portfy - Sistema Modular de Simulação Financeira

O **Portfy** é um sistema em C# voltado para a gestão de orçamento pessoal e simulação de investimentos em carteira, em transição da sua estrutura inicial para uma **Web API RESTful** (.NET) preparada para integração com um frontend em **React + TypeScript**.

---

## Integrantes do Grupo

- Carlos Florenzano
- Lucas Gomes
- Sérgio Lucas Gomes
- Arthur Salonikio
- Karina Braga Louzada
- Miguel Martins

---

## Log de Alterações e Arquitetura (28/09/2026)

### 1. Justificativa de Engenharia de Software

Adotou-se a **implementação estrutural do projeto web de forma incremental**. A migração do projeto é realizada por fases bem definidas — isolando a camada de domínio (`Models/`), ajustando a segurança para requisições concorrentes (`Thread Safety`), implementando a camada de API HTTP (`Controllers/`) e documentando a base do código —, o que garante estabilidade antes da integração final com a interface do usuário.

---

### 2. Principais Atualizações e Reestruturação

* **Reorganização de Namespaces**: Reestruturação e padronização completa dos namespaces do projeto para alinhamento direto com a hierarquia física de diretórios e pastas (`Controllers`, `Models`, `Services`).
* **Transição para o Projeto Web**: Evolução da arquitetura para ASP.NET Core Web API, readequando o core financeiro para suportar concorrência e preparar a estrutura de recebimento que atenderá ao frontend em definitivo.
* **Construção da Camada de Controllers**: Criação dos controllers RESTful (`AporteController`, `CarteiraController`, `OrcamentoController`, `UsuarioController` e `AtivoController`), expondo os recursos do sistema através de rotas HTTP com verbos apropriados e payloads JSON.
* **Mapeamento e Documentação Extensa**: Análise e documentação aprofundada de todos os modelos de domínio, regras de negócio e mecanismos de sincronização (`lock` / `Monitor`) até o momento desta atualização.

---

### 3. Roteiro e Próximos Passos (Transição Web API & Frontend)

- [x] **Consolidação do Domain Core**: Validação e isolamento das entidades de domínio e regras de negócio (`Models/`).
- [x] **Ajustes de Concorrência**: Preparação do domínio para requisições paralelas via HTTP (`lock` / Thread Safety).
- [x] **Exposição dos Endpoints REST**: Implementação e validação da camada de `Controllers` (`CarteiraController`, `AporteController`, `OrcamentoController`, `UsuarioController`, `AtivoController`) para servir payloads padronizados em JSON.
- [x] **Integração com Frontend**: Desenvolvimento da interface SPA reativa em **React + TypeScript**, pronta para consumir a base de endpoints REST estruturada na Web API.
- [ ] **Finalização do MVP Web**: Finalização da primeira interface web utilizável, com o objetivo de realizar o primeiro Release.