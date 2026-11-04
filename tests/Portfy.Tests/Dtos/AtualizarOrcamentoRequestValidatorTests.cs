using Portfy.Dtos.Orcamento;

namespace Portfy.Tests.Dtos;

public class AtualizarOrcamentoRequestValidatorTests
{
    private readonly AtualizarOrcamentoRequestValidator _validador = new();

    [Fact]
    public void Aceita_NomeAusente()
    {
        // Contrato anterior preservado: sem nome, só teto/percentual são alterados.
        var requisicao = new AtualizarOrcamentoRequest
        {
            LimiteDefinido = 100m,
            PercentualDoSalario = 10m,
        };

        Assert.True(_validador.Validate(requisicao).IsValid);
    }

    [Fact]
    public void Aceita_NomeValido()
    {
        var requisicao = new AtualizarOrcamentoRequest
        {
            NomeCategoria = "Moradia",
            LimiteDefinido = 100m,
            PercentualDoSalario = 10m,
        };

        Assert.True(_validador.Validate(requisicao).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_NomeVazio(string nome)
    {
        var requisicao = new AtualizarOrcamentoRequest
        {
            NomeCategoria = nome,
            LimiteDefinido = 100m,
            PercentualDoSalario = 10m,
        };

        Assert.False(_validador.Validate(requisicao).IsValid);
    }

    [Fact]
    public void Recusa_NomeAcimaDoLimite()
    {
        var requisicao = new AtualizarOrcamentoRequest
        {
            NomeCategoria = new string('a', 61),
            LimiteDefinido = 100m,
            PercentualDoSalario = 10m,
        };

        Assert.False(_validador.Validate(requisicao).IsValid);
    }
}
