using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Data.Seed;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Carteira persistida no SQLite e isolada por usuário (arch §9.1). A carteira
/// padrão nasce no cadastro com o saldo inicial simulado; a partir daí saldo e
/// posições só mudam pelos métodos de domínio de CarteiraSimulada, com o
/// resultado gravado por SaveChanges. Registrado como Scoped.
public sealed class CarteiraService : ICarteiraService
{
    private readonly AppDbContext _db;

    public CarteiraService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public CarteiraSimulada ObterCarteira(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(usuarioId));

        return ConsultarCarteira(usuarioId) ?? CriarCarteiraPadrao(usuarioId);
    }

    public OperacaoCarteira? Comprar(int usuarioId, string ticker, int quantidade)
    {
        CarteiraSimulada carteira = ObterCarteira(usuarioId);
        Ativo? ativo = ObterAtivo(ticker);

        if (ativo is null)
            return null;

        decimal precoUnitario = ativo.PrecoAtual;

        // O domínio valida saldo e quantidade; a persistência confirma o estado final.
        carteira.ComprarAtivo(ativo, quantidade);
        _db.SaveChanges();

        return new OperacaoCarteira(
            carteira,
            ativo.Ticker,
            quantidade,
            precoUnitario,
            precoUnitario * quantidade);
    }

    public OperacaoCarteira? Vender(int usuarioId, string ticker, int quantidade)
    {
        CarteiraSimulada carteira = ObterCarteira(usuarioId);
        Ativo? ativo = ObterAtivo(ticker);

        if (ativo is null)
            return null;

        decimal precoUnitario = ativo.PrecoAtual;

        // Vender zera a posição e ela sai da coleção; o mapeamento em cascade remove a linha.
        carteira.VenderAtivo(ativo, quantidade);
        _db.SaveChanges();

        return new OperacaoCarteira(
            carteira,
            ativo.Ticker,
            quantidade,
            precoUnitario,
            precoUnitario * quantidade);
    }

    private CarteiraSimulada? ConsultarCarteira(int usuarioId) =>
        _db.CarteirasSimuladas
            // Duas coleções carregadas: consultas separadas evitam o join cartesiano
            // (posições x aportes) que o provedor relacional alerta por padrão.
            .AsSplitQuery()
            .Include(c => c.Posicoes)
                .ThenInclude(p => p.Ativo)
            .Include(c => c.Aportes)
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Id)
            .FirstOrDefault();

    private Ativo? ObterAtivo(string ticker) =>
        // COLLATE NOCASE na coluna Ticker preserva a busca sem diferenciar caixa.
        _db.Ativos.FirstOrDefault(a => a.Ticker == ticker);

    private CarteiraSimulada CriarCarteiraPadrao(int usuarioId)
    {
        if (!_db.Usuarios.Any(u => u.Id == usuarioId))
            throw new KeyNotFoundException("Usuário não encontrado.");

        var carteira = new CarteiraSimulada(
            usuarioId,
            SeedInicial.NomeCarteiraPadrao,
            SeedInicial.SaldoInicialCarteira);

        _db.CarteirasSimuladas.Add(carteira);

        try
        {
            _db.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Corrida entre duas primeiras requisições: a outra criou a carteira
            // padrão. O índice único (UsuarioId, Nome) garante que só uma sobreviva.
            _db.Entry(carteira).State = EntityState.Detached;

            CarteiraSimulada? existente = ConsultarCarteira(usuarioId);

            if (existente is not null)
                return existente;

            throw;
        }

        return carteira;
    }
}
