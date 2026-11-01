using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento do orçamento. Limite, gasto e percentual guardam a mesma precisão
/// declarada pelas demais propriedades monetárias (18,2).
public sealed class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("Orcamentos");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        builder.Property(o => o.NomeCategoria).IsRequired().HasMaxLength(100);
        builder.Property(o => o.LimiteDefinido).HasPrecision(18, 2);
        builder.Property(o => o.PercentualDoSalario).HasPrecision(18, 2);
        builder.Property(o => o.ValorGastoAtual).HasPrecision(18, 2);
    }
}
