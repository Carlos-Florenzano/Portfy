using System.Net;
using System.Net.Http.Json;

namespace Portfy.Tests.Api;

/// O cadastro é anônimo e cada chamada executa BCrypt (custo de CPU). Sem limite
/// por IP, cadastros em massa saturam o servidor. Este teste fixa a política:
/// 10 por minuto, excedente em 429 com Retry-After.
public class CadastroRateLimitTests
{
    [Fact]
    public async Task Cadastro_AcimaDoLimitePorMinuto_Retorna429ComRetryAfter()
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage? ultima = null;

        for (int tentativa = 1; tentativa <= 11; tentativa++)
        {
            ultima = await cliente.PostAsJsonAsync("/api/usuario/cadastro", new
            {
                nome = "Usuário Limite",
                salarioMensal = 1000m,
                login = $"limite{tentativa}@portfy.test",
                senha = "segredo123",
            });

            if (tentativa < 11)
                Assert.Equal(HttpStatusCode.Created, ultima.StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
        Assert.True(ultima.Headers.Contains("Retry-After"));
    }
}
