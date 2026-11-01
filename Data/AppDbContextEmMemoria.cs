using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Portfy.Data;

/// Cria um AppDbContext sobre um banco SQLite em memória exclusivo desta instância.
///
/// Existe para os construtores sem parâmetros usados pela suíte de testes: eles
/// exercitam o mesmo caminho de persistência (EF Core + migrations) do serviço em
/// produção, sem depender de um arquivo portfy.db físico e sem manter estado
/// paralelo em coleções C#.
///
/// Cada chamada abre uma conexão própria (Data Source=:memory:), então o banco é
/// isolado por instância e o esquema é criado pela mesma migration InitialCreate
/// (EnsureCreated continua proibido — arch §12.1).
internal static class AppDbContextEmMemoria
{
    internal static AppDbContext Criar()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        // A conexão é aberta fora do EF, então o interceptor não é acionado:
        // os PRAGMAs (foreign_keys, busy_timeout, ...) são aplicados aqui.
        SqlitePragmaInterceptor.Aplicar(conexao);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conexao)
            .Options;

        var contexto = new AppDbContext(options);
        contexto.Database.Migrate();

        return contexto;
    }
}
