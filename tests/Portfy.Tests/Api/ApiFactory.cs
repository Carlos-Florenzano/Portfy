using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portfy.Tests.Api;

/// Sobe a API real (pipeline do Program.cs) com banco SQLite temporário e
/// configuração de JWT de teste. Cada instância tem banco e rate limiter próprios,
/// então os testes não compartilham estado.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _caminhoBanco =
        Path.Combine(Path.GetTempPath(), $"portfy-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Pooling=False: o pool do Microsoft.Data.Sqlite mantém o arquivo aberto
        // depois do Dispose e, no Windows, o File.Delete do _caminhoBanco falharia
        // com "being used by another process". Sem pool, cada conexão fecha de fato.
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            $"Data Source={_caminhoBanco};Pooling=False");

        // Emissor/audiência/chave de teste: a chave real nunca vive no repositório.
        builder.UseSetting("Auth:Jwt:Chave", new string('t', 64));
        builder.UseSetting("Auth:Jwt:Emissor", "Portfy.Tests");
        builder.UseSetting("Auth:Jwt:Audiencia", "Portfy.Tests");
        builder.UseSetting("Auth:Jwt:MinutosDeValidade", "30");

        // Menor custo aceito pelo SenhaHasher: os testes ficam rápidos sem burlar a regra.
        builder.UseSetting("Auth:Senha:FatorDeTrabalho", "10");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        foreach (string sufixo in new[] { string.Empty, "-shm", "-wal" })
        {
            string caminho = $"{_caminhoBanco}{sufixo}";

            if (File.Exists(caminho))
                File.Delete(caminho);
        }
    }
}
