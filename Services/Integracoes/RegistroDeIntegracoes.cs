using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Portfy.Services.Contracts;

namespace Portfy.Services.Integracoes;

/// Registro de DI das fontes de mercado (ADR-020). Fica junto das integrações
/// porque as três fontes compartilham o mesmo padrão de cliente tipado; a
/// composição continua sendo disparada por Program.cs, que é o único ponto de
/// composição da aplicação (arch §7.1, item 6).
public static class RegistroDeIntegracoes
{
    public static IServiceCollection AdicionarIntegracoesDeMercado(
        this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(configuracao);

        servicos.Configure<OpcoesDeIntegracao>(configuracao.GetSection(OpcoesDeIntegracao.Secao));

        // Cache em memória: protege a cota diária do plano Basic da FMP.
        servicos.AddMemoryCache();
        servicos.AddSingleton<IRelogio, RelogioDoSistema>();
        servicos.AddSingleton<DisjuntorDeProvedores>();

        servicos.AddHttpClient<ProvedorBrapi>((provedor, cliente) =>
        {
            OpcoesDeIntegracao opcoes = provedor.GetRequiredService<IOptions<OpcoesDeIntegracao>>().Value;
            ConfigurarCliente(cliente, opcoes.UrlBrapi, opcoes.SegundosDeTimeout);
        });

        servicos.AddHttpClient<ProvedorFmp>((provedor, cliente) =>
        {
            OpcoesDeIntegracao opcoes = provedor.GetRequiredService<IOptions<OpcoesDeIntegracao>>().Value;
            ConfigurarCliente(cliente, opcoes.UrlFmp, opcoes.SegundosDeTimeout);
        });

        servicos.AddHttpClient<ProvedorBcb>((provedor, cliente) =>
        {
            OpcoesDeIntegracao opcoes = provedor.GetRequiredService<IOptions<OpcoesDeIntegracao>>().Value;
            ConfigurarCliente(cliente, opcoes.UrlBcb, opcoes.SegundosDeTimeout);
        });

        // A ordem de registro do contrato de cotação é só desempate: a prioridade
        // real vem do tipo de ticker (BRAPI para B3, FMP para global).
        servicos.AddTransient<IProvedorDeCotacoes>(provedor => provedor.GetRequiredService<ProvedorBrapi>());
        servicos.AddTransient<IProvedorDeCotacoes>(provedor => provedor.GetRequiredService<ProvedorFmp>());
        servicos.AddTransient<IProvedorDeFundamentos>(provedor => provedor.GetRequiredService<ProvedorFmp>());
        servicos.AddTransient<IProvedorDeIndicadores>(provedor => provedor.GetRequiredService<ProvedorBcb>());

        servicos.AddScoped<IMercadoService, MercadoService>();

        return servicos;
    }

    private static void ConfigurarCliente(HttpClient cliente, string url, int segundosDeTimeout)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? endereco))
            throw new InvalidOperationException(
                $"A URL da integração '{url}' não é um endereço absoluto válido em {OpcoesDeIntegracao.Secao}.");

        cliente.BaseAddress = endereco;
        cliente.Timeout = TimeSpan.FromSeconds(segundosDeTimeout);
    }
}
