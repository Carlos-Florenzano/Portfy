using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfy.Controllers;
using Portfy.Data;
using Portfy.Dtos.Carteira;
using Portfy.Models;
using Portfy.Services;
using Portfy.Services.Contracts;

namespace Portfy.Tests.Controllers;

/// Prova de que a abstração tem consumidor injetado: o controller é exercitado
/// com um dublê de ICarteiraService, sem banco e sem estado global.
public class CarteiraControllerTests
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    private sealed class CarteiraServiceFake : ICarteiraService
    {
        private readonly CarteiraSimulada _carteira;

        public CarteiraServiceFake(CarteiraSimulada carteira) => _carteira = carteira;

        public OperacaoCarteira? ProximaOperacao { get; set; }

        public CarteiraSimulada ObterCarteira(int usuarioId) => _carteira;

        public OperacaoCarteira? Comprar(int usuarioId, string ticker, int quantidade) => ProximaOperacao;

        public OperacaoCarteira? Vender(int usuarioId, string ticker, int quantidade) => ProximaOperacao;
    }

    private static CarteiraController CriarController(ICarteiraService servico, int? usuarioId = 1)
    {
        var controller = new CarteiraController(servico);

        var claims = new List<Claim>();

        if (usuarioId.HasValue)
        {
            claims.Add(new Claim(
                ClaimsPortfy.Identificador,
                usuarioId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "teste")),
            },
        };

        return controller;
    }

    [Fact]
    public void ObterCarteira_MontaARespostaAPartirDoServicoInjetado()
    {
        var carteira = new CarteiraSimulada(usuarioId: 1, nome: "Principal", saldoInicial: 500m);
        var controller = CriarController(new CarteiraServiceFake(carteira));

        var resultado = Assert.IsType<OkObjectResult>(controller.ObterCarteira());
        string json = JsonSerializer.Serialize(resultado.Value, OpcoesJson);

        Assert.Contains("\"patrimonioTotal\":500", json);
        Assert.Contains("\"saldoDisponivel\":500", json);
        Assert.Contains("\"nome\":\"Principal\"", json);
    }

    [Fact]
    public void ObterCarteira_SemIdentidadeNoToken_Retorna403()
    {
        var carteira = new CarteiraSimulada(usuarioId: 1, nome: "Principal", saldoInicial: 500m);
        var controller = CriarController(new CarteiraServiceFake(carteira), usuarioId: null);

        Assert.IsType<ForbidResult>(controller.ObterCarteira());
    }

    [Fact]
    public void Comprar_AtivoInexistente_Retorna404()
    {
        var carteira = new CarteiraSimulada(usuarioId: 1, nome: "Principal", saldoInicial: 500m);
        var controller = CriarController(new CarteiraServiceFake(carteira));

        var resultado = Assert.IsType<ObjectResult>(
            controller.Comprar(new OperacaoCarteiraRequest { Ticker = "XPTO", Quantidade = 1 }));

        Assert.Equal(StatusCodes.Status404NotFound, resultado.StatusCode);
    }

    [Fact]
    public void Comprar_MapeiaOsValoresDaOperacao()
    {
        var carteira = new CarteiraSimulada(usuarioId: 1, nome: "Principal", saldoInicial: 615m);
        var fake = new CarteiraServiceFake(carteira)
        {
            ProximaOperacao = new OperacaoCarteira(carteira, "PETR4", 10, 38.50m, 385m),
        };

        var controller = CriarController(fake);
        var resultado = Assert.IsType<OkObjectResult>(
            controller.Comprar(new OperacaoCarteiraRequest { Ticker = "PETR4", Quantidade = 10 }));

        string json = JsonSerializer.Serialize(resultado.Value, OpcoesJson);

        Assert.Contains("\"mensagem\":\"Compra realizada com sucesso!\"", json);
        Assert.Contains("\"ticker\":\"PETR4\"", json);
        Assert.Contains("\"precoUnitario\":38.5", json);
        Assert.Contains("\"saldoDisponivel\":615", json);
    }

    [Fact]
    public void Construtor_RecusaServicoNulo()
    {
        Assert.Throws<ArgumentNullException>(() => new CarteiraController(null!));
    }

    [Fact]
    public void CarteiraService_ComecaComOSaldoInicialPadraoDoUsuario()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conexao)
            .Options;

        using var contexto = new AppDbContext(options);
        contexto.Database.Migrate();

        var usuario = new UsuarioService(contexto, new SenhaHasher())
            .Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123");

        var service = new CarteiraService(contexto);

        CarteiraSimulada carteira = service.ObterCarteira(usuario.Id);

        Assert.Equal(10000m, carteira.SaldoDisponivel);
        Assert.Equal("Principal", carteira.Nome);
    }
}
