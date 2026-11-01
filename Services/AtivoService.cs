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
