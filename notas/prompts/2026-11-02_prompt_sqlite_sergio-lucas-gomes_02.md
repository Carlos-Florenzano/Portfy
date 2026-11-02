Atue como um desenvolvedor Full Stack sênior especializado em C# .NET, React, TypeScript e SQLite.

Preciso implementar ajustes visuais, integrações e novos fluxos no projeto. Siga rigorosamente a stack e as diretrizes abaixo:

---

### 🛠️ Stack Tecnológica
- **Backend:** .NET C# (Web API / Minimal APIs)
- **Frontend:** React + TypeScript (com Tailwind CSS)
- **Banco de Dados:** SQLite
- **Bibliotecas:** `lucide-react` (para iconografia) e `echarts` / `echarts-for-react` (para gráficos)

---

### 📌 Tarefas e Requisitos

#### 1. Ajustes de UI / Layout
- **Espaçamento de Ações:** No menu/perfil do usuário, aumente o espaçamento visual (margem/gap) entre o botão **"Sair"** e o botão **"Excluir Conta"** para evitar cliques acidentais e melhorar a ergonomia da interface.
- **Iconografia (`lucide-react`):** Substitua todos os emojis e imagens rasterizadas (PNG/JPG) soltas nos botões e menus pelos ícones vetoriais correspondentes da biblioteca `lucide-react`.

> ⚠️ **ATENÇÃO - REFERÊNCIA DE DESIGN / IMAGENS:**
> Consulte as imagens e mockups de referência disponíveis no diretório:
> As imagens se encontram na pasta notas/etc/img_frontend 
> Deixa as imagens com cores que dão sintonia.

---

#### 2. Implementação Funcional dos Módulos (Frontend & Backend)
Torne funcionais os seguintes módulos e botões, criando os endpoints necessários no .NET, as migrações/entidades no SQLite e os componentes e serviços de requisição correspondentes no React:

1. **Salário e Orçamento:** 
   - Gerenciamento de renda mensal/salário e configuração de tetos de orçamentos por categoria.
2. **Investimentos:** 
   - Cadastro e listagem de ativos/carteira do usuário.
3. **Aportes:** 
   - Registro de histórico e novos fluxos de depósitos/compras de ativos.
4. **Relatórios:** 
   - Visualização e exportação/consolidação dos dados financeiros por período.

---

#### 3. Refatoração do Dashboard (Orçamentos & Gráficos)
- **Gráfico de Orçamentos (ECharts):** Converta a exibição dos orçamentos no Dashboard para um gráfico de pizza interativo (Donut/Pie Chart) utilizando **ECharts**.
- **Tratamento de Estado Vazio (Empty State):** 
  - Remova a mensagem de placeholder: *"Os orçamentos serão exibidos aqui quando o backend disponibilizar esses dados"*.
  - No lugar, quando não existirem orçamentos cadastrados, renderize um componente limpo de Empty State (ex.: um ícone do Lucide com texto convidando o usuário a cadastrar seu primeiro orçamento ou uma área neutra de onboarding).

---

### 📦 Entregáveis Esperados
1. Código das rotas/endpoints no backend (.NET) com models e persistência no SQLite.
2. Componentes e serviços correspondentes em React + TypeScript.
3. Configuração do gráfico via ECharts com tipagem TypeScript adequada.

# Diretrizes para Execução
- Siga os padrões arquiteturais já estabelecidos no repositório (verificados através da leitura da pasta `docs`).
- Certifique-se de que o código compilável e sem erros de sintaxe ou dependências ausentes seja entregue.
- Mantenha o histórico do Git organizado caso vá commitar as alterações (opcional, mas recomendado).