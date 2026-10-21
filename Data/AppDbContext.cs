using Microsoft.EntityFrameworkCore;
using Portfy.Models;

namespace Portfy.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Tabelas que serão criadas no SQLite com base nas classes, porém é necessário acertar futuramente
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Ativo> Ativos => Set<Ativo>();
    public DbSet<CarteiraSimulada> CarteirasSimuladas => Set<CarteiraSimulada>();
    public DbSet<AporteSimulado> AportesSimulados => Set<AporteSimulado>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}