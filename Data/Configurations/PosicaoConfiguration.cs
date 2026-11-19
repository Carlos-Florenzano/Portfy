using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento da posição. Associação com Ativo por chave estrangeira (o ativo tem vida
/// independente da carteira), impedindo excluir um ativo ainda referenciado.
/// As FKs são explícitas (arch §9.1) e a coluna da carteira mantém o nome histórico.
public sealed class PosicaoConfiguration : IEntityTypeConfiguration<Posicao>
{
    public void Configure(EntityTypeBuilder<Posicao> builder)
    {
        builder.ToTable("Posicoes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.CarteiraId)
            .HasColumnName("CarteiraSimuladaId")
            .IsRequired();
        builder.Property(p => p.AtivoId).IsRequired();

        builder.Property(p => p.Quantidade).IsRequired();
        builder.Property(p => p.PrecoMedio).HasPrecision(18, 2);

        // Uma carteira não pode ter duas posições do mesmo ativo.
        builder.HasIndex(p => new { p.CarteiraId, p.AtivoId }).IsUnique();

        builder.HasOne(p => p.Ativo)
            .WithMany()
            .HasForeignKey(p => p.AtivoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.Ativo).IsRequired();
    }
}
