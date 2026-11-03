using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Catálogo de ativos e simulação de variação de preço.
/// O catálogo agora vive no SQLite (seed da migration) e o preço simulado é
/// persistido a cada simulação, sobrevivendo a reinícios da API.
/// Registrado como Scoped porque depende do AppDbContext.
public sealed class AtivoService : IAtivoService
{
    private const decimal VariacaoMinimaPercentual = -5m;
    private const decimal VariacaoMaximaPercentual = 5m;

    private readonly AppDbContext _db;
    private readonly IGeradorDeVariacao _geradorDeVariacao;

    public AtivoService(AppDbContext db, IGeradorDeVariacao geradorDeVariacao)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _geradorDeVariacao = geradorDeVariacao
            ?? throw new ArgumentNullException(nameof(geradorDeVariacao));
    }

    // Seam da suíte de testes: banco SQLite em memória isolado, mesmo caminho de persistência.
    public AtivoService(IGeradorDeVariacao geradorDeVariacao)
        : this(AppDbContextEmMemoria.Criar(), geradorDeVariacao)
    {
    }

    public IReadOnlyCollection<Ativo> ObterTodos() => _db.Ativos.AsNoTracking().ToList();

    public Ativo? ObterPorTicker(string ticker) =>
        // COLLATE NOCASE na coluna Ticker preserva a busca sem diferenciar caixa.
        _db.Ativos.FirstOrDefault(a => a.Ticker == ticker);

    public Ativo Criar(string ticker, string nome, string tipo, decimal precoAtual)
    {
        if (!Enum.TryParse(tipo, ignoreCase: true, out TipoAtivo tipoAtivo))
            throw new ArgumentException(
                "O tipo deve ser Acao, Fii, RendaFixa, Cripto ou Etf.",
                nameof(tipo));

        // Cada subtipo tem sua própria invariante; o serviço só escolhe o construtor,
        // nunca replica validação de domínio. Taxas não informadas começam em zero.
        Ativo ativo = tipoAtivo switch
        {
            TipoAtivo.Acao => new Acao(ticker, nome, precoAtual, dividendYieldAnual: 0m),
            TipoAtivo.Fii => new FundoImobiliario(ticker, nome, precoAtual, rendimentoMensalPorCota: 0m),
            TipoAtivo.RendaFixa => new RendaFixa(ticker, nome, precoAtual, taxaAnualContratada: 0m),
            TipoAtivo.Cripto => new Cripto(ticker, nome, precoAtual, taxaStakingAnual: 0m),
            TipoAtivo.Etf => new Etf(ticker, nome, precoAtual, indiceReferencia: "Não informado", taxaAdministracaoAnual: 0m),
            _ => throw new ArgumentException("Tipo de ativo não suportado.", nameof(tipo)),
        };

        _db.Ativos.Add(ativo);
        _db.SaveChanges();

        return ativo;
    }

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

        // Regra de negócio permanece no domínio; aqui só persistimos o resultado.
        ativo.AtualizarPreco(novoPreco);
        _db.SaveChanges();

        return new VariacaoSimulada(ativo, precoAnterior);
    }
}
