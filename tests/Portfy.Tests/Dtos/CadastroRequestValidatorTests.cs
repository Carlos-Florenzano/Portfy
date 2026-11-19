using Portfy.Dtos.Usuario;

namespace Portfy.Tests.Dtos;

public class CadastroRequestValidatorTests
{
    private readonly CadastroRequestValidator _validador = new();

    private static CadastroRequest CriarRequisicaoValida() => new()
    {
        Nome = "Ana Souza",
        SalarioMensal = 3000m,
        Login = "ana@portfy.test",
        Senha = "segredo123",
    };

    [Fact]
    public void Aceita_PayloadCompleto()
    {
        Assert.True(_validador.Validate(CriarRequisicaoValida()).IsValid);
    }

    [Fact]
    public void Recusa_NomeVazio()
    {
        CadastroRequest requisicao = CriarRequisicaoValida();
        requisicao.Nome = " ";

        var resultado = _validador.Validate(requisicao);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, falha => falha.PropertyName == nameof(CadastroRequest.Nome));
    }

    [Fact]
    public void Recusa_NomeCurto()
    {
        CadastroRequest requisicao = CriarRequisicaoValida();
        requisicao.Nome = "Ab";

        Assert.False(_validador.Validate(requisicao).IsValid);
    }

    [Fact]
    public void Recusa_LoginCurto()
    {
        CadastroRequest requisicao = CriarRequisicaoValida();
        requisicao.Login = "ab";

        var resultado = _validador.Validate(requisicao);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, falha => falha.PropertyName == nameof(CadastroRequest.Login));
    }

    [Fact]
    public void Recusa_SalarioNegativo()
    {
        CadastroRequest requisicao = CriarRequisicaoValida();
        requisicao.SalarioMensal = -1m;

        var resultado = _validador.Validate(requisicao);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, falha => falha.PropertyName == nameof(CadastroRequest.SalarioMensal));
    }

    [Fact]
    public void Recusa_SenhaCurta()
    {
        CadastroRequest requisicao = CriarRequisicaoValida();
        requisicao.Senha = "1234567";

        Assert.False(_validador.Validate(requisicao).IsValid);
    }

    [Fact]
    public void Recusa_SenhaAcimaDoLimiteDoBcrypt()
    {
        CadastroRequest requisicao = CriarRequisicaoValida();
        requisicao.Senha = new string('a', 65);

        Assert.False(_validador.Validate(requisicao).IsValid);
    }
}
