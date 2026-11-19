using Portfy.Dtos.Ativo;

namespace Portfy.Tests.Dtos;

public class SimularVariacaoRequestValidatorTests
{
    private readonly SimularVariacaoRequestValidator _validador = new();

    [Fact]
    public void Aceita_PercentualAusente()
    {
        Assert.True(_validador.Validate(new SimularVariacaoRequest()).IsValid);
    }

    [Fact]
    public void Aceita_PercentualDentroDaFaixa()
    {
        Assert.True(_validador.Validate(new SimularVariacaoRequest { PercentualCustomizado = 10m }).IsValid);
        Assert.True(_validador.Validate(new SimularVariacaoRequest { PercentualCustomizado = -50m }).IsValid);
    }

    [Fact]
    public void Recusa_PercentualQueZeraOPreco()
    {
        Assert.False(_validador.Validate(new SimularVariacaoRequest { PercentualCustomizado = -100m }).IsValid);
    }

    [Fact]
    public void Recusa_PercentualAcimaDoLimite()
    {
        Assert.False(_validador.Validate(new SimularVariacaoRequest { PercentualCustomizado = 1001m }).IsValid);
    }
}
