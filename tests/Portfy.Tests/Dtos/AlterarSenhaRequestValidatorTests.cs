using Portfy.Dtos.Usuario;

namespace Portfy.Tests.Dtos;

public class AlterarSenhaRequestValidatorTests
{
    private readonly AlterarSenhaRequestValidator _validador = new();

    [Fact]
    public void Aceita_NovaSenhaDiferente()
    {
        var requisicao = new AlterarSenhaRequest
        {
            SenhaAtual = "segredo123",
            NovaSenha = "outra-senha",
        };

        Assert.True(_validador.Validate(requisicao).IsValid);
    }

    [Fact]
    public void Recusa_NovaSenhaIgualARecente()
    {
        var requisicao = new AlterarSenhaRequest
        {
            SenhaAtual = "segredo123",
            NovaSenha = "segredo123",
        };

        var resultado = _validador.Validate(requisicao);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            falha => falha.PropertyName == nameof(AlterarSenhaRequest.NovaSenha));
    }

    [Fact]
    public void Recusa_NovaSenhaCurta()
    {
        var requisicao = new AlterarSenhaRequest
        {
            SenhaAtual = "segredo123",
            NovaSenha = "1234567",
        };

        Assert.False(_validador.Validate(requisicao).IsValid);
    }

    [Fact]
    public void Recusa_SenhaAtualVazia()
    {
        var requisicao = new AlterarSenhaRequest
        {
            SenhaAtual = " ",
            NovaSenha = "outra-senha",
        };

        Assert.False(_validador.Validate(requisicao).IsValid);
    }
}
