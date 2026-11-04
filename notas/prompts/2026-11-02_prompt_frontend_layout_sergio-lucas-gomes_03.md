Você está integrado ao meu projeto no VS Code e tem acesso a todos os arquivos da solução.

Preciso que você ajuste a funcionalidade de edição de categorias na tela "Salário e orçamento" (seção "Categorias") para permitir que o usuário também altere o NOME da categoria, além dos campos que já podem ser editados hoje (Limite, % do Salário e Gasto fixo).

### Diretrizes Visuais e de Layout (MUITO IMPORTANTE):
- NÃO mude o tema, padrão de cores, tipografia, bordas nem a estrutura visual Tailwind existente.
- O campo de input do novo "Nome da categoria" deve seguir exatamente o mesmo padrão visual e de estilo dos inputs já existentes nessa linha (como os campos "LIMITE (R$)" e "CATEGORIA" do card "Novo orçamento").
- Organize o layout da linha de edição para acomodar o novo campo de forma harmônica e responsiva (por exemplo: Nome da categoria, Limite, % do Salário, Checkbox Gasto fixo e os botões Salvar / Cancelar).

---

### O que precisa ser feito:

1. **Frontend (React + TypeScript):**
   - Localize o componente responsável pela listagem e edição de categorias (na tela de "Salário e orçamento").
   - No modo de edição da categoria (quando o usuário clica no botão "Editar"):
     - Adicione um input com o label "CATEGORIA" ou "NOME" carregando o nome atual da categoria como valor inicial.
     - Permita a edição do texto.
     - Vincule o novo valor ao estado do formulário de edição.
     - Ao clicar em "Salvar", envie o nome atualizado junto com os outros campos (limite, percentual, gasto fixo) para a requisição de atualização.
     - Ao clicar em "Cancelar", reverta qualquer alteração voltando ao estado original de visualização.

2. **Backend (ASP.NET Core Web API C# .NET 10 + SQLite):**
   - Verifique o DTO de atualização da categoria/orçamento (ex: `AtualizarCategoriaDto` / `UpdateOrcamentoDto`) e a rota correspondente (geralmente `PUT /api/orcamentos/{id}` ou `PUT /api/categorias/{id}`).
   - Certifique-se de que o DTO aceite o campo `Nome` (ou renomear) e que a validação permita a alteração (validando regras existentes, como nome não vazio e unicidade por usuário se aplicável).
   - Atualize a entidade no banco de dados SQLite persistindo o novo nome da categoria.

Analise os arquivos existentes, faça as alterações necessárias mantendo estritamente o padrão da nossa base de código e liste os arquivos modificados.

# Diretrizes para Execução
- Siga os padrões arquiteturais já estabelecidos no repositório (verificados através da leitura da pasta `docs`).
- Certifique-se de que o código compilável e sem erros de sintaxe ou dependências ausentes seja entregue.
- Mantenha o histórico do Git organizado caso vá commitar as alterações (opcional, mas recomendado).