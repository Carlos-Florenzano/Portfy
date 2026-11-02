using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Data.Seed;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Orçamento persistido no SQLite e isolado por usuário (arch §9.1): o orçamento
/// padrão nasce no cadastro e cada despesa é somada pelo método de domínio
/// AdicionarDespesa e gravada por SaveChanges. Registrado como Scoped.
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

    public void AdicionarDespesa(int usuarioId, decimal valor)
    {
        Orcamento orcamento = ObterOrcamento(usuarioId);

        orcamento.AdicionarDespesa(valor);

        _db.SaveChanges();
    }

    private Orcamento? ConsultarOrcamento(int usuarioId) =>
        _db.Orcamentos
            .Where(o => o.UsuarioId == usuarioId)
            .OrderBy(o => o.Id)
            .FirstOrDefault();

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
