using Portfy.Data;
using Portfy.Data.Seed;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Orçamento persistido no SQLite: o orçamento padrão vem do seed da migration e
/// cada despesa é somada pelo método de domínio AdicionarDespesa e gravada por
/// SaveChanges. Registrado como Scoped porque depende do AppDbContext.
public sealed class OrcamentoService : IOrcamentoService
{
    private readonly AppDbContext _db;

    public OrcamentoService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Orcamento ObterOrcamento() =>
        _db.Orcamentos.FirstOrDefault(o => o.Id == SeedInicial.OrcamentoPadraoId)
        ?? throw new InvalidOperationException(
            "O orçamento padrão não foi inicializado. Verifique se as migrations foram aplicadas.");

    public void AdicionarDespesa(decimal valor)
    {
        Orcamento orcamento = ObterOrcamento();

        orcamento.AdicionarDespesa(valor);

        _db.SaveChanges();
    }
}
