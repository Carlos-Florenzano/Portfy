using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento do usuário. O login é único e comparado sem diferenciar caixa
/// (COLLATE NOCASE), preservando a regra de domínio de LoginJaExiste.
public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Nome).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Login).IsRequired().HasMaxLength(200).UseCollation("NOCASE");
        builder.HasIndex(u => u.Login).IsUnique();

        // Continua sendo o hash PBKDF2 (salt:hash em Base64); a senha em texto puro nunca é persistida.
        builder.Property(u => u.SenhaHash).IsRequired().HasMaxLength(400);

        builder.Property(u => u.SalarioMensal).HasPrecision(18, 2);
        builder.Property(u => u.DataRecebimento).IsRequired();
        builder.Property(u => u.ContaVerificada).IsRequired();
    }
}
