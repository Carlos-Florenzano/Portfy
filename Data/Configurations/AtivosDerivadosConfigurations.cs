using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfy.Models;

namespace Portfy.Data.Configurations;

// Configuração dos campos exclusivos de cada subtipo do TPH de Ativo.
// Ficam em classes próprias porque o builder de Ativo não alcança propriedades
// declaradas apenas nas subclasses.

public sealed class AcaoConfiguration : IEntityTypeConfiguration<Acao>
{
    public void Configure(EntityTypeBuilder<Acao> builder)
    {
        builder.Property(a => a.DividendYieldAnual).HasPrecision(18, 2);
    }
}

public sealed class FundoImobiliarioConfiguration : IEntityTypeConfiguration<FundoImobiliario>
{
    public void Configure(EntityTypeBuilder<FundoImobiliario> builder)
    {
        builder.Property(f => f.RendimentoMensalPorCota).HasPrecision(18, 2);
    }
}

public sealed class CriptoConfiguration : IEntityTypeConfiguration<Cripto>
{
    public void Configure(EntityTypeBuilder<Cripto> builder)
    {
        builder.Property(c => c.TaxaStakingAnual).HasPrecision(18, 2);
    }
}

public sealed class EtfConfiguration : IEntityTypeConfiguration<Etf>
{
    public void Configure(EntityTypeBuilder<Etf> builder)
    {
        builder.Property(e => e.IndiceReferencia).HasMaxLength(100);
        builder.Property(e => e.TaxaAdministracaoAnual).HasPrecision(18, 2);
    }
}

public sealed class RendaFixaConfiguration : IEntityTypeConfiguration<RendaFixa>
{
    public void Configure(EntityTypeBuilder<RendaFixa> builder)
    {
        builder.Property(r => r.TaxaAnualContratada).HasPrecision(18, 2);
    }
}
