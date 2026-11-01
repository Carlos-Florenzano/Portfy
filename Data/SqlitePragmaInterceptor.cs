using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Portfy.Data;

/// Aplica os PRAGMAs exigidos sempre que o EF abre uma conexão SQLite.
///
/// Executar por conexão (e não uma única vez no startup) é o que garante que
/// foreign_keys, busy_timeout e synchronous valham em todas as conexões do pool,
/// inclusive sob requisições HTTP concorrentes, onde cada requisição usa a sua.
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    private const string Pragmas =
        "PRAGMA foreign_keys = ON;" +
        "PRAGMA journal_mode = WAL;" +
        "PRAGMA busy_timeout = 5000;" +
        "PRAGMA synchronous = NORMAL;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => Aplicar(connection);

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Aplicar(connection);
        return Task.CompletedTask;
    }

    /// Executa os PRAGMAs em uma conexão já aberta. Usado pelo interceptor e pelo
    /// contexto em memória, que abre a conexão fora do EF.
    public static void Aplicar(DbConnection connection)
    {
        using DbCommand comando = connection.CreateCommand();
        comando.CommandText = Pragmas;
        comando.ExecuteNonQuery();
    }
}
