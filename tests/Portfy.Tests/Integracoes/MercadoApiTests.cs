using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portfy.Dtos.Mercado;
using Portfy.Services.Contracts;
using Portfy.Tests.Api;

namespace Portfy.Tests.Integracoes;

/// Borda HTTP das rotas de mercado: contrato JSON e rate limit por IP. O serviço
/// é substituído por um dublê, então nenhum teste aqui toca a rede.
public class MercadoApiTests
{
    private sealed class MercadoServiceFalso : IMercadoService
    {
        public Task<CotacaoResponse> ObterCotacaoAsync(
            string ticker,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CotacaoResponse
            {
                Ticker = ticker,
                Nome = "Ativo de teste",
                Preco = 10m,
                Moeda = "BRL",
                Fonte = OrigemDeMercado.Brapi.ToString(),
            });

        public Task<FundamentoResponse> ObterFundamentosAsync(
            string ticker,
            CancellationToken cancellationToken) =>
            Task.FromResult(new FundamentoResponse
            {
                Ticker = ticker,
                Nome = "Empresa de teste",
                Cobertura = CoberturaDeDados.Completa,
                Fonte = OrigemDeMercado.Fmp.ToString(),
            });

        public Task<IndicadoresResponse> ObterIndicadoresAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new IndicadoresResponse
            {
                Indicadores = new[]
                {
                    new IndicadorResponse
                    {
                        Codigo = "IPCA",
                        Descricao = "IPCA — variação mensal",
                        Valor = 0.82m,
                        Data = new DateOnly(2026, 9, 1),
                        Unidade = "% no mês",
                        SerieSgs = "433",
                        Fonte = OrigemDeMercado.BcbSgs.ToString(),
                    },
                },
            });
    }

    private static HttpClient CriarCliente(out WebApplicationFactory<Program> fabrica)
    {
        fabrica = new ApiFactory().WithWebHostBuilder(construcao =>
            construcao.ConfigureTestServices(servicos =>
            {
                servicos.RemoveAll<IMercadoService>();
                servicos.AddSingleton<IMercadoService, MercadoServiceFalso>();
            }));

        return fabrica.CreateClient();
    }

    [Fact]
    public async Task Cotacao_RespondeOContratoPadronizado()
    {
        using HttpClient cliente = CriarCliente(out WebApplicationFactory<Program> fabrica);
        using (fabrica)
        {
            HttpResponseMessage resposta = await cliente.GetAsync("/api/mercado/cotacoes/PETR4");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            CotacaoResponse? cotacao = await resposta.Content.ReadFromJsonAsync<CotacaoResponse>();
            Assert.NotNull(cotacao);
            Assert.Equal("PETR4", cotacao!.Ticker);
            Assert.Equal(10m, cotacao.Preco);
            Assert.Equal("Brapi", cotacao.Fonte);
        }
    }

    [Fact]
    public async Task Indicadores_RespondeAsSeriesDoBancoCentral()
    {
        using HttpClient cliente = CriarCliente(out WebApplicationFactory<Program> fabrica);
        using (fabrica)
        {
            HttpResponseMessage resposta = await cliente.GetAsync("/api/mercado/indicadores");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            IndicadoresResponse? indicadores =
                await resposta.Content.ReadFromJsonAsync<IndicadoresResponse>();

            Assert.NotNull(indicadores);
            IndicadorResponse ipca = Assert.Single(indicadores!.Indicadores);
            Assert.Equal("IPCA", ipca.Codigo);
            Assert.Equal("BcbSgs", ipca.Fonte);
        }
    }

    [Fact]
    public async Task Mercado_AcimaDoLimitePorIpResponde429ComRetryAfter()
    {
        using HttpClient cliente = CriarCliente(out WebApplicationFactory<Program> fabrica);
        using (fabrica)
        {
            HttpResponseMessage ultima = new(HttpStatusCode.OK);

            // A política de mercado permite 60 consultas por minuto por IP.
            for (int tentativa = 1; tentativa <= 61; tentativa++)
                ultima = await cliente.GetAsync("/api/mercado/indicadores");

            Assert.Equal(HttpStatusCode.TooManyRequests, ultima.StatusCode);
            Assert.True(ultima.Headers.Contains("Retry-After"));
        }
    }
}
