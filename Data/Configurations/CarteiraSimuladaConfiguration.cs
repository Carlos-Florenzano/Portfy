using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento da carteira. Posicoes e Aportes continuam em listas privadas
/// (_posicoes / _aportes); o EF escreve nelas por backing field, sem expor setter público.
public sealed class CarteiraSimuladaConfiguration : IEntityTypeConfiguration<CarteiraSimulada>
{
    public void Configure(EntityTypeBuilder<CarteiraSimulada> builder)
    {
        builder.ToTable("Carteiras");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.SaldoDisponivel).HasPrecision(18, 2);

        // Composição: a posição nasce e morre com a carteira.
        builder.HasMany(c => c.Posicoes)
            .WithOne()
            .HasForeignKey("CarteiraSimuladaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Posicoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Composição: o aporte pertence à carteira, mas não é apagado por engano
        // (histórico financeiro não é descartado em cascata).
        builder.HasMany(c => c.Aportes)
            .WithOne()
            .HasForeignKey("CarteiraSimuladaId")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(c => c.Aportes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
