using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Data.Seed;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Orçamento persistido no SQLite e isolado por usuário (arch §9.1): as categorias
/// nascem no cadastro e cada despesa é somada pelo método de domínio AdicionarDespesa
/// e gravada por SaveChanges. Registrado como Scoped.
public sealed class OrcamentoService : IOrcamentoService
{
    private readonly AppDbContext _db;

    public OrcamentoService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Orcamento ObterOrcamento(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(usuarioId));

        return ConsultarOrcamento(usuarioId) ?? CriarOrcamentoPadrao(usuarioId);
    }

    public IReadOnlyCollection<Orcamento> ObterTodos(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(usuarioId));

        return _db.Orcamentos
            .AsNoTracking()
            .Where(o => o.UsuarioId == usuarioId)
            .OrderBy(o => o.Id)
            .ToList();
    }

    public Orcamento? ObterPorId(int usuarioId, int orcamentoId)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(usuarioId));

        return _db.Orcamentos
            .AsNoTracking()
            .FirstOrDefault(o => o.Id == orcamentoId && o.UsuarioId == usuarioId);
    }

    public Orcamento Criar(
        int usuarioId,
        string nomeCategoria,
        decimal limiteDefinido,
        decimal percentualDoSalario,
        bool ehFixo)
    {
        if (!_db.Usuarios.Any(u => u.Id == usuarioId))
            throw new KeyNotFoundException("Usuário não encontrado.");

        // A duplicidade de categoria é barrada pelo índice único (UsuarioId,
        // NomeCategoria); o middleware traduz a violação em 409.
        var orcamento = new Orcamento(
            usuarioId,
            nomeCategoria,
            limiteDefinido,
            percentualDoSalario,
            ehFixo);

        _db.Orcamentos.Add(orcamento);
        _db.SaveChanges();

        return orcamento;
    }

    public Orcamento? Atualizar(
        int usuarioId,
        int orcamentoId,
        string? nomeCategoria,
        decimal limiteDefinido,
        decimal percentualDoSalario,
        bool ehFixo)
    {
        Orcamento? orcamento = ConsultarPorId(usuarioId, orcamentoId);

        if (orcamento is null)
            return null;

        // Nome nulo mantém o contrato anterior (só teto/percentual/recorrência).
        if (nomeCategoria is not null)
            orcamento.Renomear(nomeCategoria);

        orcamento.AtualizarDefinicao(limiteDefinido, percentualDoSalario, ehFixo);

        // Colisão de nome é barrada pelo índice único (UsuarioId, NomeCategoria);
        // o middleware traduz a violação em 409.
        _db.SaveChanges();

        return orcamento;
    }

    public bool Excluir(int usuarioId, int orcamentoId)
    {
        Orcamento? orcamento = ConsultarPorId(usuarioId, orcamentoId);

        if (orcamento is null)
            return false;

        _db.Orcamentos.Remove(orcamento);
        _db.SaveChanges();

        return true;
    }

    public void AdicionarDespesa(int usuarioId, decimal valor)
    {
        Orcamento orcamento = ObterOrcamento(usuarioId);

        orcamento.AdicionarDespesa(valor);

        _db.SaveChanges();
    }

    public Orcamento? AdicionarDespesa(int usuarioId, int orcamentoId, decimal valor)
    {
        Orcamento? orcamento = ConsultarPorId(usuarioId, orcamentoId);

        if (orcamento is null)
            throw new KeyNotFoundException("Orçamento não encontrado.");

        orcamento.AdicionarDespesa(valor);

        _db.SaveChanges();

        return orcamento;
    }

    private Orcamento? ConsultarOrcamento(int usuarioId) =>
        _db.Orcamentos
            .Where(o => o.UsuarioId == usuarioId)
            .OrderBy(o => o.Id)
            .FirstOrDefault();

    private Orcamento? ConsultarPorId(int usuarioId, int orcamentoId) =>
        _db.Orcamentos
            .FirstOrDefault(o => o.Id == orcamentoId && o.UsuarioId == usuarioId);

    private Orcamento CriarOrcamentoPadrao(int usuarioId)
    {
        if (!_db.Usuarios.Any(u => u.Id == usuarioId))
            throw new KeyNotFoundException("Usuário não encontrado.");

        var orcamento = new Orcamento(
            usuarioId,
            SeedInicial.CategoriaPadrao,
            SeedInicial.LimitePadrao,
            SeedInicial.PercentualPadrao);

        _db.Orcamentos.Add(orcamento);

        try
        {
            _db.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Corrida entre duas primeiras requisições: a outra criou a categoria
            // padrão. O índice único (UsuarioId, NomeCategoria) resolve a disputa.
            _db.Entry(orcamento).State = EntityState.Detached;

            Orcamento? existente = ConsultarOrcamento(usuarioId);

            if (existente is not null)
                return existente;

            throw;
        }

        return orcamento;
    }
}
