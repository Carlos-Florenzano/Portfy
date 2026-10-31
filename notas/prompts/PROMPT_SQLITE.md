# Implementação da Persistência SQLite — Projeto Portfy

Atue como um Arquiteto de Software e Engenheiro Backend Sênior especializado em ASP.NET Core, .NET 8, C#, Entity Framework Core e SQLite.

Preciso implementar uma camada de persistência real utilizando SQLite e Entity Framework Core no projeto **Portfy**, migrando o estado volátil em memória para um banco de dados relacional.

O objetivo é implementar essa funcionalidade preservando a arquitetura existente, as regras de negócio, os contratos dos endpoints e os comportamentos já cobertos pelos **61 testes de unidade existentes**.

## 1. Etapa obrigatória: diagnóstico antes da implementação

Antes de modificar qualquer arquivo:

1. Analise toda a estrutura do projeto.
2. Examine `Program.cs`, `AppDbContext.cs`, todos os Models, Services e Controllers.
3. Identifique os construtores, propriedades, setters privados, campos privados e métodos responsáveis pelas alterações de estado.
4. Examine os 61 testes existentes e identifique os comportamentos que precisam ser preservados.
5. Consulte o estado atual do `AtivoService` para recuperar os dados originais do catálogo.
6. Identifique todos os serviços registrados no container de injeção de dependência.
7. Apresente um plano de implementação com os arquivos que serão modificados e os motivos.

Não presuma que propriedades, relacionamentos ou construtores existem sem verificar o código-fonte.

## 2. Configuração do SQLite

Configure a persistência utilizando:

- Entity Framework Core compatível com .NET 10.
- Provedor Microsoft.EntityFrameworkCore.Sqlite.
- Banco de dados `portfy.db`.
- String de conexão configurada em `appsettings.json`.
- `AppDbContext` registrado como Scoped.

Configure adequadamente os PRAGMAs necessários:

- foreign_keys = ON;
- journal_mode = WAL;
- busy_timeout = 5000;
- synchronous = NORMAL.

Garanta que a configuração seja compatível com o ciclo de vida das conexões SQLite e com requisições HTTP concorrentes.

## 3. Mapeamento do AppDbContext

Implemente o mapeamento utilizando Fluent API em `OnModelCreating`.

### Usuario
- Chave primária autoincrementada.
- Login com índice único.
- Campos obrigatórios conforme as regras atuais do domínio.
- Salário e demais propriedades monetárias corretamente mapeados.
- Preservação do armazenamento de senha exclusivamente por PBKDF2.

### Hierarquia Ativo
Utilize TPH (Table-per-Hierarchy), com discriminador para:

- Ativo;
- Acao;
- FundoImobiliario;
- Cripto;
- Etf;
- RendaFixa.

Preserve os campos específicos de cada subclasse e suas regras polimórficas.

### CarteiraSimulada e Posicao
- Configure o relacionamento 1:N.
- Analise o mapeamento de campos privados e backing fields.
- Preserve `_posicoes` e `_aportes`.
- Preserve os métodos de domínio responsáveis pelas compras, vendas e atualização do preço médio.
- Não altere as regras de cálculo existentes.

Atenção: `Posicao` está declarada dentro de `Models/CarteiraSimulada.cs`. Não utilize a classe comentada existente em `Models/Posicao.cs`.

### AporteSimulado
- Configure a chave primária.
- Mapeie valor e data.
- Configure o relacionamento com Usuario por meio de `OrigemSalarioId`.
- Implemente integridade referencial.
- Não invente propriedades de navegação inexistentes sem justificar a alteração arquitetural.

### Orcamento
Mapeie:
- Nome da categoria;
- Limite definido;
- Percentual do salário;
- Valor gasto atual.

Preserve os métodos e validações existentes.

## 4. Persistência de todos os serviços

Não implemente a persistência exclusivamente no UsuarioService.

Analise e adapte os serviços responsáveis por manter estado:

- UsuarioService;
- CarteiraService;
- AporteService;
- AtivoService;
- OrcamentoService.

O GeradorDeVariacaoAleatoria deve ser analisado separadamente, sem criar persistência desnecessária.

Todos os serviços que utilizarem AppDbContext devem possuir ciclo de vida compatível com Scoped.

Substitua as coleções em memória que representam dados persistentes por operações com Entity Framework Core.

Utilize operações assíncronas, como:

- ToListAsync();
- FirstOrDefaultAsync();
- FindAsync();
- SaveChangesAsync().

Preserve os contratos públicos dos serviços e dos controllers sempre que possível.

Não altere:
- Rotas HTTP;
- Estrutura dos DTOs;
- Códigos de resposta;
- Mensagens de validação;
- Regras de negócio;
- Algoritmo PBKDF2;
- Comportamento esperado pelos testes existentes.

## 5. Integridade e relacionamentos

Analise a necessidade de relacionamentos entre:

- Usuario e CarteiraSimulada;
- Usuario e Orcamento;
- Usuario e AporteSimulado;
- CarteiraSimulada e Posicao;
- CarteiraSimulada e AporteSimulado;
- Posicao e Ativo.

Não crie relacionamentos arbitrários.

Se a implementação exigir novas propriedades ou alterações nos Models, explique a necessidade e preserve os invariantes do domínio.

Evite que dados de usuários diferentes sejam compartilhados acidentalmente.

Defina comportamentos de exclusão referencial adequados, evitando apagar históricos financeiros indevidamente.

## 6. Seed inicial do catálogo

Implemente a inicialização dos quatro ativos existentes no catálogo mockado:

- PETR4 — Ação, DY de 12%;
- VALE3 — Ação, DY de 8,5%;
- HGLG11 — Fundo Imobiliário, rendimento de R$ 1,10 por cota;
- BTC — Cripto, taxa de staking de 0%.

Consulte os valores originais, nomes, preços e parâmetros existentes nos construtores e no AtivoService.

Não invente preços ou informações.

O seed deve ser idempotente: reiniciar a API não pode duplicar os ativos nem sobrescrever alterações legítimas realizadas posteriormente.

## 7. Precisão monetária

Preserve `decimal` no domínio C#.

Analise as limitações do SQLite para operações com decimal e implemente uma estratégia segura de persistência.

Garanta que:
- Valores monetários não sofram perda de precisão;
- Operações financeiras mantenham o arredondamento definido pelo domínio;
- A leitura e gravação preservem os valores;
- Comparações e cálculos não dependam de comportamentos incompatíveis do SQLite.

Justifique a estratégia escolhida.

## 8. Compatibilidade com os testes

Os 61 testes existentes são um requisito obrigatório de preservação.

Antes da implementação, execute:

`dotnet test tests/Portfy.Tests`

Após cada etapa relevante, execute novamente a suíte.

Se os testes utilizarem serviços em memória, mocks ou construtores específicos, preserve a possibilidade de testá-los sem depender de um arquivo SQLite físico.

Não altere ou remova testes apenas para fazer a implementação passar.

Se algum teste precisar de adaptação por uma mudança arquitetural inevitável, justifique a alteração e mantenha a cobertura do comportamento original.

## 9. Migrations

Crie as migrations utilizando a CLI do .NET.

Apresente os comandos necessários:

`dotnet ef migrations add InitialCreate`

`dotnet ef database update`

Verifique a estrutura gerada, as chaves, os índices, os relacionamentos e os discriminadores.

Garanta que o banco seja criado corretamente e que os dados iniciais sejam inseridos.

## 10. Entregáveis

Apresente:

1. AppDbContext.cs completo e comentado.
2. Program.cs atualizado.
3. appsettings.json atualizado.
4. Todos os Services adaptados à persistência.
5. Models modificados, caso necessário, com justificativa.
6. Configuração de seed.
7. Migration inicial.
8. Comandos para criação e atualização do banco.
9. Estratégia de testes.
10. Relatório final das alterações.

## 11. Regras obrigatórias de implementação

- Não reescreva o projeto inteiro.
- Não remova funcionalidades existentes.
- Não simplifique regras de negócio.
- Não substitua métodos de domínio por operações diretas no banco.
- Não exponha SenhaHash em respostas HTTP.
- Não altere contratos dos endpoints sem autorização.
- Não utilize Singleton para serviços que dependem do DbContext.
- Não considere a implementação concluída enquanto existirem serviços mantendo estado persistente exclusivamente em memória.
- Não invente propriedades ou dados ausentes.
- Faça alterações pequenas, organizadas e verificáveis.

**Critério final de aprovação:**

O projeto deve compilar sem erros, os 61 testes existentes devem continuar aprovados e os dados persistidos no SQLite devem permanecer disponíveis após reiniciar a API.

Ao finalizar, apresente uma lista dos arquivos modificados, as decisões arquiteturais tomadas, os testes executados e quaisquer limitações ou funcionalidades que ainda não tenham sido implementadas.