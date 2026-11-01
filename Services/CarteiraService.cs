using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Data.Seed;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Carteira persistida no SQLite (arch §9). A carteira padrão e o seu saldo inicial
/// vêm do seed da migration; a partir daí o saldo e as posições só mudam pelos
/// métodos de domínio de CarteiraSimulada, com o resultado gravado por SaveChanges.
/// Registrado como Scoped porque depende do AppDbContext.
public sealed class CarteiraService : ICarteiraService
{
    private readonly AppDbContext _db;

    public CarteiraService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // Seam da suíte de testes: banco SQLite em memória isolado, mesmo caminho de persistência.
    public CarteiraService() : this(AppDbContextEmMemoria.Criar())
    {
    }

    public CarteiraSimulada ObterCarteira() =>
        _db.CarteirasSimuladas
            // Duas coleções carregadas: consultas separadas evitam o join cartesiano
            // (posições x aportes) que o provedor relacional alerta por padrão.
            .AsSplitQuery()
            .Include(c => c.Posicoes)
                .ThenInclude(p => p.Ativo)
            .Include(c => c.Aportes)
            .FirstOrDefault(c => c.Id == SeedInicial.CarteiraPadraoId)
        ?? throw new InvalidOperationException(
            "A carteira padrão não foi inicializada. Verifique se as migrations foram aplicadas.");
}
