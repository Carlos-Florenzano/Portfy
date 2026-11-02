using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento da carteira. Posicoes e Aportes continuam em listas privadas
/// (_posicoes / _aportes); o EF escreve nelas por backing field, sem expor setter público.
/// A carteira pertence a um usuário (arch §9.1) e o nome da simulação é único por dono.
public sealed class CarteiraSimuladaConfiguration : IEntityTypeConfiguration<CarteiraSimulada>
{
    public void Configure(EntityTypeBuilder<CarteiraSimulada> builder)
    {
        builder.ToTable("Carteiras");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.UsuarioId).IsRequired();
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(100);
        builder.Property(c => c.SaldoDisponivel).HasPrecision(18, 2);

        // Um usuário pode ter várias simulações, mas não duas com o mesmo nome.
        builder.HasIndex(c => new { c.UsuarioId, c.Nome }).IsUnique();

        // Excluir a conta remove as carteiras do usuário; o histórico de aportes
        // é tratado explicitamente pelo UsuarioService antes desta exclusão.
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composição: a posição nasce e morre com a carteira.
        builder.HasMany(c => c.Posicoes)
            .WithOne()
            .HasForeignKey(p => p.CarteiraId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Posicoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Composição: o aporte pertence à carteira, mas não é apagado por engano
        // (histórico financeiro não é descartado em cascata).
        builder.HasMany(c => c.Aportes)
            .WithOne()
            .HasForeignKey(a => a.CarteiraId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Aportes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
