using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento da posição. Associação com Ativo por chave estrangeira (o ativo tem vida
/// independente da carteira), impedindo excluir um ativo ainda referenciado.
public sealed class PosicaoConfiguration : IEntityTypeConfiguration<Posicao>
{
    public void Configure(EntityTypeBuilder<Posicao> builder)
    {
        builder.ToTable("Posicoes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Quantidade).IsRequired();
        builder.Property(p => p.PrecoMedio).HasPrecision(18, 2);

        builder.HasOne(p => p.Ativo)
            .WithMany()
            .HasForeignKey("AtivoId")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.Ativo).IsRequired();
    }
}
