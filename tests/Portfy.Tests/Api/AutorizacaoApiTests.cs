using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Portfy.Tests.Api;

/// Bateria de tentativas de ataque contra a borda HTTP: acesso cruzado a dados de
/// outro usuário (IDOR), token adulterado e carga de injeção SQL. O contrato é
/// sempre o mesmo: recusar com 401/403/404 — nunca 500 e nunca vazar dado alheio.
public class AutorizacaoApiTests
{
    private const string SenhaPadrao = "segredo123";

    private sealed record UsuarioCriado(int Id);

    private sealed record UsuarioDoToken(int Id);

    private sealed record LoginRealizado(string Token, UsuarioDoToken Usuario);

    private sealed record CarteiraDaApi(int Id);

    private sealed record OrcamentoDaApi(int Id);

    private static async Task<(HttpClient Cliente, int UsuarioId)> CriarUsuarioAutenticadoAsync(
        ApiFactory fabrica,
        string login)
    {
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage cadastro = await cliente.PostAsJsonAsync("/api/usuario/cadastro", new
        {
            nome = "Usuário de Teste",
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

        return (cliente, criado.Id);
    }

    [Theory]
    [InlineData("/api/carteira")]
    [InlineData("/api/orcamento")]
    [InlineData("/api/orcamento/todos")]
    [InlineData("/api/aporte")]
    [InlineData("/api/relatorio")]
    [InlineData("/api/relatorio/csv")]
    public async Task EndpointsComDadoFinanceiro_SemToken_Retornam401(string rota)
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync(rota);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Ativo_PostSemToken_Retorna401()
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/ativo", new
        {
            ticker = "XXXX3",
            nome = "Ativo Injetado",
            tipo = "Acao",
            precoAtual = 10m,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);

        // O catálogo não pode ter sido alterado pela tentativa.
        HttpResponseMessage consulta = await cliente.GetAsync("/api/ativo/XXXX3");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task Orcamento_DeOutroUsuario_Retorna404()
    {
        using var fabrica = new ApiFactory();
        var (clienteAna, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");
        var (clienteBruno, _) = await CriarUsuarioAutenticadoAsync(fabrica, "bruno@portfy.test");

        OrcamentoDaApi orcamentoDoBruno =
            (await clienteBruno.GetFromJsonAsync<OrcamentoDaApi>("/api/orcamento"))!;

        HttpResponseMessage leitura = await clienteAna.GetAsync($"/api/orcamento/{orcamentoDoBruno.Id}");
        HttpResponseMessage alteracao = await clienteAna.PutAsJsonAsync($"/api/orcamento/{orcamentoDoBruno.Id}", new
        {
            limiteDefinido = 1m,
            percentualDoSalario = 1m,
            ehFixo = false,
        });
        HttpResponseMessage remocao = await clienteAna.DeleteAsync($"/api/orcamento/{orcamentoDoBruno.Id}");
        HttpResponseMessage despesa = await clienteAna.PostAsJsonAsync(
            $"/api/orcamento/{orcamentoDoBruno.Id}/despesa",
            new { valor = 1m });

        Assert.Equal(HttpStatusCode.NotFound, leitura.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, alteracao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, remocao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, despesa.StatusCode);

        // O orçamento do Bruno continua intacto e acessível para ele.
        HttpResponseMessage conferencia = await clienteBruno.GetAsync($"/api/orcamento/{orcamentoDoBruno.Id}");
        Assert.Equal(HttpStatusCode.OK, conferencia.StatusCode);
    }

    [Fact]
    public async Task Aporte_ComCarteiraDeOutroUsuario_Retorna404()
    {
        using var fabrica = new ApiFactory();
        var (clienteAna, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");
        var (clienteBruno, idBruno) = await CriarUsuarioAutenticadoAsync(fabrica, "bruno@portfy.test");

        CarteiraDaApi carteiraDoBruno =
            (await clienteBruno.GetFromJsonAsync<CarteiraDaApi>("/api/carteira"))!;

        HttpResponseMessage resposta = await clienteAna.PostAsJsonAsync("/api/aporte", new
        {
            valor = 500m,
            carteiraId = carteiraDoBruno.Id,
        });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        // Nem o saldo do Bruno nem o histórico dele foram alterados.
        CarteiraDaApi depois =
            (await clienteBruno.GetFromJsonAsync<CarteiraDaApi>($"/api/carteira"))!;

        Assert.Equal(carteiraDoBruno.Id, depois.Id);
        Assert.True(idBruno > 0);
    }

    [Fact]
    public async Task TokenComAssinaturaAdulterada_Retorna401()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        string tokenValido = cliente.DefaultRequestHeaders.Authorization!.Parameter!;
        char ultimo = tokenValido[^1];
        char trocado = ultimo == 'A' ? 'B' : 'A';
        string tokenAdulterado = tokenValido[..^1] + trocado;

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenAdulterado);

        HttpResponseMessage resposta = await cliente.GetAsync("/api/carteira");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenComLixoNoLugarDoJwt_Retorna401()
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "nao-e-um-jwt");

        HttpResponseMessage resposta = await cliente.GetAsync("/api/relatorio");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("' OR 1=1 --")]
    [InlineData("PETR4'--")]
    [InlineData("\" OR \"1\"=\"1")]
    public async Task CargaDeInjecaoSqlNoLogin_Retorna401SemErroInterno(string login)
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/usuario/login", new
        {
            login,
            senha = SenhaPadrao,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("' OR 1=1 --")]
    [InlineData("PETR4%27%20OR%20%271%27%3D%271")]
    public async Task CargaDeInjecaoSqlNoTicker_Retorna404SemErroInterno(string ticker)
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync($"/api/ativo/{Uri.EscapeDataString(ticker)}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task CorpoMalformadoNoCadastro_Retorna400SemErroInterno()
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/usuario/cadastro", new
        {
            nome = "A",
            salarioMensal = -1m,
            login = "ab",
            senha = "123",
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task VerificacaoDeConta_NaoExpoeSeOIdExisteParaAnonimo()
    {
        using var fabrica = new ApiFactory();
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.PostAsync("/api/usuario/999999/verificar", null);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.DoesNotContain("stack", await resposta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
