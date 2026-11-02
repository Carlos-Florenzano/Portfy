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

        // Hash BCrypt (60 caracteres); a coluna mantém 400 para aceitar também os
        // hashes PBKDF2 antigos (salt:hash em Base64) até serem regravados. A senha
        // em texto puro nunca é persistida.
        builder.Property(u => u.SenhaHash).IsRequired().HasMaxLength(400);

        builder.Property(u => u.SalarioMensal).HasPrecision(18, 2);
        builder.Property(u => u.DataRecebimento).IsRequired();
        builder.Property(u => u.ContaVerificada).IsRequired();
    }
}
