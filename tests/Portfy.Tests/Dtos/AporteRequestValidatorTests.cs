using Portfy.Dtos.Aporte;
using Portfy.Dtos.Orcamento;

namespace Portfy.Tests.Dtos;

public class AporteRequestValidatorTests
{
    private readonly AporteRequestValidator _validador = new();

    [Fact]
    public void Aceita_ValorEOrigemPositivos()
    {
        Assert.True(_validador.Validate(new AporteRequest { Valor = 100m, OrigemSalarioId = 1 }).IsValid);
    }

    [Fact]
    public void Recusa_ValorNaoPositivo()
    {
        var resultado = _validador.Validate(new AporteRequest { Valor = 0m, OrigemSalarioId = 1 });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, falha => falha.PropertyName == nameof(AporteRequest.Valor));
    }

    [Fact]
    public void Recusa_OrigemSalarioInvalida()
    {
        var resultado = _validador.Validate(new AporteRequest { Valor = 100m, OrigemSalarioId = 0 });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, falha => falha.PropertyName == nameof(AporteRequest.OrigemSalarioId));
    }

    [Fact]
    public void Despesa_RecusaValorNaoPositivo()
    {
        var validadorDespesa = new AdicionarDespesaRequestValidator();

        Assert.True(validadorDespesa.Validate(new AdicionarDespesaRequest { Valor = 10m }).IsValid);
        Assert.False(validadorDespesa.Validate(new AdicionarDespesaRequest { Valor = 0m }).IsValid);
    }
}
