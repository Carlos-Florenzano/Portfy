using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento do aporte. A integridade referencial aponta para o usuário dono do
/// salário de origem (OrigemSalarioId) e para a carteira que recebeu o valor.
/// Excluir o usuário não apaga o histórico financeiro (a remoção da conta faz isso
/// explicitamente no serviço, em transação única).
public sealed class AporteSimuladoConfiguration : IEntityTypeConfiguration<AporteSimulado>
{
    public void Configure(EntityTypeBuilder<AporteSimulado> builder)
    {
        builder.ToTable("Aportes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.ValorAportado).HasPrecision(18, 2);
        builder.Property(a => a.DataAporte).IsRequired();
        builder.Property(a => a.OrigemSalarioId).IsRequired();

        // Coluna histórica mantida para não renomear dados existentes.
        builder.Property(a => a.CarteiraId)
            .HasColumnName("CarteiraSimuladaId")
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(a => a.OrigemSalarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
