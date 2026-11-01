using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento do aporte. A integridade referencial aponta para o usuário dono do
/// salário de origem (OrigemSalarioId); excluir o usuário não apaga o histórico financeiro.
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

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(a => a.OrigemSalarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Vínculo de composição com a carteira, criado pelo mapeamento em
        // CarteiraSimuladaConfiguration. É anulável de propósito: um aporte pode ser
        // registrado sem estar preso a uma carteira (o modelo de domínio atual não
        // tem CarteiraId em AporteSimulado), e o vínculo nunca apaga histórico.
        builder.Property<int?>("CarteiraSimuladaId");
    }
}
