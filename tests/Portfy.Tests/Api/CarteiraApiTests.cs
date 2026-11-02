using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Portfy.Tests.Api;

/// Prova de ponta a ponta da carteira por usuário: cadastro cria carteira e
/// orçamento no SQLite, compra/venda alteram saldo e posição, aporte soma ao
/// saldo e um usuário nunca enxerga os dados do outro (arch §9.1).
public class CarteiraApiTests
{
    private const string SenhaPadrao = "segredo123";

    private sealed record UsuarioCriado(int Id);

    private sealed record UsuarioDoToken(int Id, string Nome, string Login);

    private sealed record LoginRealizado(string Token, DateTime ExpiraEm, UsuarioDoToken Usuario);

    private sealed record AtivoDaPosicao(
        string Ticker,
        string Nome,
        string Tipo,
        decimal PrecoAtual,
        decimal RentabilidadeAcumulada);

    private sealed record PosicaoDaCarteira(
        int Id,
        AtivoDaPosicao Ativo,
        int Quantidade,
        decimal PrecoMedio);

    private sealed record AporteDaCarteira(
        int Id,
        DateTime DataAporte,
        decimal ValorAportado,
        int OrigemSalarioId,
        int CarteiraId);

    private sealed record CarteiraDaApi(
        int Id,
        string Nome,
        decimal PatrimonioTotal,
        decimal SaldoDisponivel,
        List<PosicaoDaCarteira> Posicoes,
        List<AporteDaCarteira> Aportes);

    private sealed record OperacaoDaApi(
        string Mensagem,
        string Ticker,
        int Quantidade,
        decimal PrecoUnitario,
        decimal ValorTotal,
        decimal SaldoDisponivel,
        decimal PatrimonioTotal);

    private sealed record AporteCriadoDaApi(
        string Mensagem,
        AporteDaCarteira Aporte,
        decimal SaldoDisponivel,
        decimal PatrimonioTotal);

    private sealed record OrcamentoDaApi(
        string NomeCategoria,
        decimal LimiteDefinido,
        decimal ValorGastoAtual,
        decimal PercentualDoSalario,
        decimal SaldoRestante,
        bool Estourado,
        bool EhFixo);

    private static async Task<(HttpClient Cliente, int UsuarioId)> CriarUsuarioAutenticadoAsync(
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

        return (cliente, criado.Id);
    }

    [Fact]
    public async Task Cadastro_CriaCarteiraEOrcamentoDoUsuario()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        CarteiraDaApi carteira =
            (await cliente.GetFromJsonAsync<CarteiraDaApi>("/api/carteira"))!;

        Assert.Equal("Principal", carteira.Nome);
        Assert.Equal(10000m, carteira.SaldoDisponivel);
        Assert.Equal(10000m, carteira.PatrimonioTotal);
        Assert.Empty(carteira.Posicoes);
        Assert.Empty(carteira.Aportes);

        OrcamentoDaApi orcamento =
            (await cliente.GetFromJsonAsync<OrcamentoDaApi>("/api/orcamento"))!;

        Assert.Equal("Alimentação", orcamento.NomeCategoria);
        Assert.Equal(1200m, orcamento.LimiteDefinido);
        Assert.Equal(0m, orcamento.ValorGastoAtual);
        Assert.False(orcamento.EhFixo);
    }

    [Fact]
    public async Task Compra_AlteraSaldoEPosicaoNoBanco()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/carteira/compras", new
        {
            ticker = "PETR4",
            quantidade = 10,
        });

        resposta.EnsureSuccessStatusCode();

        OperacaoDaApi operacao = (await resposta.Content.ReadFromJsonAsync<OperacaoDaApi>())!;

        Assert.Equal("Compra realizada com sucesso!", operacao.Mensagem);
        Assert.Equal(38.50m, operacao.PrecoUnitario);
        Assert.Equal(385m, operacao.ValorTotal);
        Assert.Equal(9615m, operacao.SaldoDisponivel);

        CarteiraDaApi carteira =
            (await cliente.GetFromJsonAsync<CarteiraDaApi>("/api/carteira"))!;

        PosicaoDaCarteira posicao = Assert.Single(carteira.Posicoes);
        Assert.Equal("PETR4", posicao.Ativo.Ticker);
        Assert.Equal(10, posicao.Quantidade);
        Assert.Equal(38.50m, posicao.PrecoMedio);
        Assert.Equal(9615m, carteira.SaldoDisponivel);
    }

    [Fact]
    public async Task Venda_ZeraAPosicaoEAumentaOSaldo()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        (await cliente.PostAsJsonAsync("/api/carteira/compras", new
        {
            ticker = "PETR4",
            quantidade = 10,
        })).EnsureSuccessStatusCode();

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/carteira/vendas", new
        {
            ticker = "PETR4",
            quantidade = 10,
        });

        resposta.EnsureSuccessStatusCode();

        OperacaoDaApi operacao = (await resposta.Content.ReadFromJsonAsync<OperacaoDaApi>())!;

        Assert.Equal("Venda realizada com sucesso!", operacao.Mensagem);
        Assert.Equal(10000m, operacao.SaldoDisponivel);

        CarteiraDaApi carteira =
            (await cliente.GetFromJsonAsync<CarteiraDaApi>("/api/carteira"))!;

        Assert.Empty(carteira.Posicoes);
        Assert.Equal(10000m, carteira.SaldoDisponivel);
    }

    [Fact]
    public async Task Aporte_SomaAoSaldoDaCarteira()
    {
        using var fabrica = new ApiFactory();
        var (cliente, usuarioId) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/aporte", new
        {
            valor = 500m,
        });

        resposta.EnsureSuccessStatusCode();

        AporteCriadoDaApi criado = (await resposta.Content.ReadFromJsonAsync<AporteCriadoDaApi>())!;

        Assert.Equal(500m, criado.Aporte.ValorAportado);
        Assert.Equal(usuarioId, criado.Aporte.OrigemSalarioId);
        Assert.True(criado.Aporte.CarteiraId > 0);
        Assert.Equal(10500m, criado.SaldoDisponivel);
        Assert.Equal(10500m, criado.PatrimonioTotal);

        CarteiraDaApi carteira =
            (await cliente.GetFromJsonAsync<CarteiraDaApi>("/api/carteira"))!;

        Assert.Equal(10500m, carteira.SaldoDisponivel);
        Assert.Single(carteira.Aportes);
    }

    [Fact]
    public async Task CarteiraDeUmUsuario_NaoVazaParaOutro()
    {
        using var fabrica = new ApiFactory();
        var (clienteAna, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");
        var (clienteBruno, _) = await CriarUsuarioAutenticadoAsync(fabrica, "bruno@portfy.test");

        (await clienteAna.PostAsJsonAsync("/api/carteira/compras", new
        {
            ticker = "PETR4",
            quantidade = 10,
        })).EnsureSuccessStatusCode();

        CarteiraDaApi carteiraBruno =
            (await clienteBruno.GetFromJsonAsync<CarteiraDaApi>("/api/carteira"))!;

        Assert.Empty(carteiraBruno.Posicoes);
        Assert.Equal(10000m, carteiraBruno.SaldoDisponivel);
    }

    [Fact]
    public async Task CompraSemSaldo_Retorna422()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/carteira/compras", new
        {
            ticker = "PETR4",
            quantidade = 1000,
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task CompraComTickerInexistente_Retorna404()
    {
        using var fabrica = new ApiFactory();
        var (cliente, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");

        HttpResponseMessage resposta = await cliente.PostAsJsonAsync("/api/carteira/compras", new
        {
            ticker = "XPTO",
            quantidade = 1,
        });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Orcamento_IsoladoPorUsuario()
    {
        using var fabrica = new ApiFactory();
        var (clienteAna, _) = await CriarUsuarioAutenticadoAsync(fabrica, "ana@portfy.test");
        var (clienteBruno, _) = await CriarUsuarioAutenticadoAsync(fabrica, "bruno@portfy.test");

        (await clienteAna.PostAsJsonAsync("/api/orcamento/despesa", new { valor = 200m }))
            .EnsureSuccessStatusCode();

        OrcamentoDaApi orcamentoAna =
            (await clienteAna.GetFromJsonAsync<OrcamentoDaApi>("/api/orcamento"))!;
        OrcamentoDaApi orcamentoBruno =
            (await clienteBruno.GetFromJsonAsync<OrcamentoDaApi>("/api/orcamento"))!;

        Assert.Equal(200m, orcamentoAna.ValorGastoAtual);
        Assert.Equal(1000m, orcamentoAna.SaldoRestante);
        Assert.Equal(0m, orcamentoBruno.ValorGastoAtual);
    }
}
