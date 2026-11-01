using Microsoft.EntityFrameworkCore;
using Portfy.Data.Seed;
using Portfy.Models;

namespace Portfy.Data;

/// Única autoridade sobre o estado persistido (arch §9): nenhum serviço mantém
/// coleções de domínio entre requisições, todo dado vive nas tabelas abaixo.
///
/// Ciclo de vida: registrado como Scoped em Program.cs, uma instância por
/// requisição HTTP. É o EF que abre e fecha a conexão SQLite, e o
/// SqlitePragmaInterceptor reaplica os PRAGMAs em cada abertura.
///
/// Decimais: o provedor SQLite grava `decimal` como TEXT, com todos os dígitos,
/// então leitura e gravação não perdem precisão. HasPrecision(18,2) documenta a
/// escala do domínio, mas nenhuma conta é feita no SQL: soma, divisão e
/// arredondamento continuam apenas nos métodos de domínio (e no
/// Math.Round(..., 2, MidpointRounding.AwayFromZero) já existente), longe das
/// limitações do SQLite para aritmética decimal e ORDER BY.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Tabelas criadas por migration (Data/Migrations) a partir dos Models.
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Ativo> Ativos => Set<Ativo>();
    public DbSet<CarteiraSimulada> CarteirasSimuladas => Set<CarteiraSimulada>();
    public DbSet<AporteSimulado> AportesSimulados => Set<AporteSimulado>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // O mapeamento fica em Data/Configurations, descoberto por assembly (arch §12.4).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Seed executado pela migration inicial (arch §12.7 / ADR-016).
        SeedInicial.Aplicar(modelBuilder);
    }
}
