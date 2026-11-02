using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Portfy.Tests.Api;

/// Prova de ponta a ponta do pipeline de autenticação: cadastro, login com JWT,
/// endpoint protegido, isolamento entre usuários e rate limiting do login.
public class AutenticacaoTests
{
    private const string SenhaPadrao = "segredo123";

    private sealed record UsuarioCriado(int Id);

    private sealed record UsuarioDoToken(int Id, string Nome, string Login);

    private sealed record LoginRealizado(string Token, DateTime ExpiraEm, UsuarioDoToken Usuario);

    private static async Task<(HttpClient Cliente, LoginRealizado Login)> CriarUsuarioAutenticadoAsync(
        ApiFactory fabrica,
        string login)
    {
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage cadastro = await cliente.PostAsJsonAsync("/api/usuario/cadastro", new
        {
            nome = "Ana Souza",
            salarioMensal = 3000m,
            login,
            senha = SenhaPadrao,
        });

        cadastro.EnsureSuccessStatusCode();

        UsuarioCriado criado = (await cadastro.Content.ReadFromJsonAsync<UsuarioCriado>())!;

        (await cliente.PostAsync($"/api/usuario/{criado.Id}/verificar", null))
            .EnsureSuccessStatusCode();

        HttpResponseMessage respostaLogin = await cliente.PostAsJsonAsync("/api/usuario/login", new
        {
            login,
            senha = SenhaPadrao,
        });

        respostaLogin.EnsureSuccessStatusCode();

        LoginRealizado loginRealizado =
            (await respostaLogin.Content.ReadFromJsonAsync<LoginRealizado>())!;

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginRealizado.Token);

        return (cliente, loginRealizado);
    }

    [Fact]
    public async Task Login_DevolveTokenComIdentidadeDoUsuario()
    {
        using var fabrica = new ApiFactory();
        var (_, login) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.True(login.ExpiraEm > DateTime.UtcNow);
        Assert.Equal("ana@portfy.test", login.Usuario.Login);
    }

    [Fact]
    public async Task EndpointProtegido_SemToken_Retorna401()
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/api/carteira");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_ComToken_Retorna200()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        HttpResponseMessage resposta = await cliente.GetAsync("/api/carteira");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Usuario_NaoAcessaDadosDeOutroUsuario()
    {
        using var fabrica = new ApiFactory();
        var (clienteAna, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");
        var (_, bruno) = await CriarUsuarioAutenticadoAsync(fabrica, "bruno@portfy.test");

        HttpResponseMessage resposta = await clienteAna.GetAsync($"/api/usuario/{bruno.Usuario.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_AcimaDoLimitePorMinuto_Retorna429ComRetryAfter()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        // A primeira autenticação acima já consumiu 1 das 10 tentativas da janela.
        HttpResponseMessage? ultima = null;

        for (int tentativa = 2; tentativa <= 11; tentativa++)
        {
            ultima = await cliente.PostAsJsonAsync("/api/usuario/login", new
            {
                login = "ana@portfy.test",
                senha = "senha-incorreta",
            });

            if (tentativa < 11)
                Assert.Equal(HttpStatusCode.Unauthorized, ultima.StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
        Assert.True(ultima.Headers.Contains("Retry-After"));
    }
}
