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

## Log de Alterações e Arquitetura (27/09/2026)

### 1. Justificativa de Engenharia de Software

Adotou-se a **implementação estrutural do projeto web de forma incremental**. A migração do projeto é realizada por fases bem definidas — isolando a camada de domínio (`Models/`), ajustando a segurança para requisições concorrentes (`Thread Safety`) e documentando a base do código —, o que garante estabilidade antes da exposição dos endpoints e da integração final com a interface do usuário.

---

### 2. Principais Atualizações e Reestruturação

* **Reorganização de Namespaces**: Reestruturação e padronização completa dos namespaces do projeto para alinhamento direto com a hierarquia física de diretórios e pastas (`Controllers`, `Models`, `Services`).
* **Transição para o Projeto Web**: Evolução da arquitetura para ASP.NET Core Web API, readequando o core financeiro para suportar concorrência e preparar a estrutura de recebimento que atenderá ao frontend em definitivo.
* **Mapeamento e Documentação Extensa**: Análise e documentação aprofundada de todos os modelos de domínio, regras de negócio e mecanismos de sincronização (`lock` / `Monitor`) até o momento desta atualização.

---

### 3. Roteiro e Próximos Passos (Transição Web API & Frontend)

- [x] **Consolidação do Domain Core**: Validação e isolamento das entidades de domínio e regras de negócio (`Models/`).
- [x] **Ajustes de Concorrência**: Preparação do domínio para requisições paralelas via HTTP (`lock` / Thread Safety).
- [ ] **Exposição dos Endpoints REST**: Finalização da implementação dos `Controllers` (`CarteiraController`, `AporteController`) para servir payloads em JSON.
- [ ] **Integração com Frontend**: Desenvolvimento de interface SPA reativa em **React + TypeScript**.