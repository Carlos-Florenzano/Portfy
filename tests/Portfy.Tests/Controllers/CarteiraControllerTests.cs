using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Portfy.Controllers;
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

        public CarteiraSimulada ObterCarteira() => _carteira;
    }

    [Fact]
    public void ObterCarteira_MontaARespostaAPartirDoServicoInjetado()
    {
        var carteira = new CarteiraSimulada(500m);
        var controller = new CarteiraController(new CarteiraServiceFake(carteira));

        var resultado = Assert.IsType<OkObjectResult>(controller.ObterCarteira());
        string json = JsonSerializer.Serialize(resultado.Value, OpcoesJson);

        Assert.Contains("\"patrimonioTotal\":500", json);
        Assert.Contains("\"saldoDisponivel\":500", json);
    }

    [Fact]
    public void Construtor_RecusaServicoNulo()
    {
        Assert.Throws<ArgumentNullException>(() => new CarteiraController(null!));
    }

    [Fact]
    public void CarteiraService_ComecaComOSaldoInicialPadrao()
    {
        var service = new CarteiraService();

        Assert.Equal(10000m, service.ObterCarteira().SaldoDisponivel);
    }
}
