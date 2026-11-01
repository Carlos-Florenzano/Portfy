using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

/// Mapeamento da hierarquia Ativo em TPH (uma tabela, discriminador em string na coluna Tipo).
/// O ticker é o identificador natural do catálogo: único e comparado sem diferenciar caixa.
public sealed class AtivoConfiguration : IEntityTypeConfiguration<Ativo>
{
    public void Configure(EntityTypeBuilder<Ativo> builder)
    {
        builder.ToTable("Ativos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.Ticker).IsRequired().HasMaxLength(20).UseCollation("NOCASE");
        builder.HasIndex(a => a.Ticker).IsUnique();

        builder.Property(a => a.Nome).IsRequired().HasMaxLength(200);
        builder.Property(a => a.PrecoAtual).HasPrecision(18, 2);
        builder.Property(a => a.RentabilidadeAcumulada).HasPrecision(18, 2);

        // Tipo é calculado por cada subclasse (polimorfismo): não vira coluna CLR.
        // A identificação da subclasse fica na coluna "Tipo", como discriminador string (ADR-009).
        // Ativo é abstrato, portanto nunca aparece como linha: o discriminador distingue
        // os cinco subtipos concretos do catálogo.
        builder.Ignore(a => a.Tipo);
        builder.HasDiscriminator<string>("TipoAtivo")
            .HasValue<Acao>("Acao")
            .HasValue<FundoImobiliario>("Fii")
            .HasValue<Cripto>("Cripto")
            .HasValue<Etf>("Etf")
            .HasValue<RendaFixa>("RendaFixa");

        builder.Property<string>("TipoAtivo").HasColumnName("Tipo").IsRequired().HasMaxLength(20);
    }
}
