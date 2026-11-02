namespace Portfy.Services.Integracoes;

/// Configuração das fontes externas, lida da seção `Integracoes`.
///
/// As credenciais ficam fora do repositório (arch §15, item 5): use a variável de
/// ambiente `Integracoes__TokenBrapi` e `Integracoes__ChaveFmp`, ou `dotnet
/// user-secrets`. Os valores vazios abaixo são o padrão seguro: sem credencial, a
/// fonte é considerada não configurada e o serviço continua no ar.
public sealed class OpcoesDeIntegracao
{
    public const string Secao = "Integracoes";

    public string UrlBrapi { get; set; } = "https://brapi.dev";

    public string TokenBrapi { get; set; } = string.Empty;

    public string UrlFmp { get; set; } = "https://financialmodelingprep.com";

    public string ChaveFmp { get; set; } = string.Empty;

    public string UrlBcb { get; set; } = "https://api.bcb.gov.br";

    /// Timeout de cada chamada externa (arch §15, item 7).
    public int SegundosDeTimeout { get; set; } = 8;

    /// Tempo em que uma fonte que falhou (429, 5xx, timeout, credencial recusada)
    /// fica fora da rotação, evitando pagar o timeout dela em cada requisição.
    public int SegundosDeBloqueioDoProvedor { get; set; } = 60;

    /// Cotações mudam rápido, mas o plano Basic da FMP tem cota diária: 60 s de
    /// cache protege a cota sem servir preço visivelmente velho.
    public int SegundosDeCacheDeCotacao { get; set; } = 60;

    /// Fundamentos mudam a cada balanço: 12 h é suficiente e economiza cota.
    public int SegundosDeCacheDeFundamento { get; set; } = 43200;

    /// Indicadores do Banco Central são publicados uma vez por mês ou por dia.
    public int SegundosDeCacheDeIndicador { get; set; } = 3600;
}
