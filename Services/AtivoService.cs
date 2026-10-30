using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Catálogo de ativos e simulação de variação de preço.
/// Registrado como Singleton porque o preço simulado precisa sobreviver entre requisições
/// até a persistência em SQLite existir (dívida P0-a).
public sealed class AtivoService : IAtivoService
{
    private const decimal VariacaoMinimaPercentual = -5m;
    private const decimal VariacaoMaximaPercentual = 5m;

    private readonly IGeradorDeVariacao _geradorDeVariacao;

    private readonly List<Ativo> _catalogo =
    [
        new Acao("PETR4", "Petrobras PN", 38.50m, 12.00m),
        new Acao("VALE3", "Vale ON", 62.10m, 8.50m),
        new FundoImobiliario("HGLG11", "CGHG Logística", 160.00m, 1.10m),
        new Cripto("BTC", "Bitcoin", 350000.00m)
    ];

    public AtivoService(IGeradorDeVariacao geradorDeVariacao)
    {
        _geradorDeVariacao = geradorDeVariacao
            ?? throw new ArgumentNullException(nameof(geradorDeVariacao));
    }

    public IReadOnlyCollection<Ativo> ObterTodos() => _catalogo.AsReadOnly();

    public Ativo? ObterPorTicker(string ticker) =>
        _catalogo.FirstOrDefault(a => a.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));

    public VariacaoSimulada? SimularVariacao(string ticker, decimal? percentualCustomizado)
    {
        Ativo? ativo = ObterPorTicker(ticker);

        if (ativo is null)
            return null;

        decimal precoAnterior = ativo.PrecoAtual;
        decimal percentual = percentualCustomizado
            ?? _geradorDeVariacao.SortearPercentual(VariacaoMinimaPercentual, VariacaoMaximaPercentual);

        decimal novoPreco = Math.Round(
            precoAnterior * (1m + (percentual / 100m)),
            2,
            MidpointRounding.AwayFromZero);

        ativo.AtualizarPreco(novoPreco);

        return new VariacaoSimulada(ativo, precoAnterior);
    }
}
