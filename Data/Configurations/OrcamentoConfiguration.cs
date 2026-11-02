using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento do orçamento. Cada usuário tem as suas categorias (arch §9.1);
/// a categoria é única por dono e os valores guardam a precisão monetária (18,2).
public sealed class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("Orcamentos");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        builder.Property(o => o.UsuarioId).IsRequired();
        builder.Property(o => o.NomeCategoria).IsRequired().HasMaxLength(100);
        builder.Property(o => o.LimiteDefinido).HasPrecision(18, 2);
        builder.Property(o => o.PercentualDoSalario).HasPrecision(18, 2);
        builder.Property(o => o.ValorGastoAtual).HasPrecision(18, 2);
        builder.Property(o => o.EhFixo).IsRequired();

        // Uma categoria não se repete para o mesmo usuário.
        builder.HasIndex(o => new { o.UsuarioId, o.NomeCategoria }).IsUnique();

        // Excluir a conta remove os orçamentos do usuário.
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(o => o.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
