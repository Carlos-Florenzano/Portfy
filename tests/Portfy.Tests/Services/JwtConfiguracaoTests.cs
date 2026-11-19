using System.Text;
using Microsoft.Extensions.Configuration;
using Portfy.Services;
using Xunit;

namespace Portfy.Tests.Services;

// A chave do JWT é a fronteira de confiança do backend: se estiver ausente ou
// curta, o startup precisa falhar em vez de operar com segredo fraco. Estes
// testes cobrem exatamente essa regra (não havia cobertura para JwtConfiguracao).
public class JwtConfiguracaoTests
{
    private const string ChaveValida = "chave-de-teste-com-32-bytes-ok!!";

    private static IConfiguration CriarConfiguracao(
        string? chave = ChaveValida,
        int? minutosDeValidade = null,
        string? emissor = null,
        string? audiencia = null)
    {
        var valores = new Dictionary<string, string?>();

        if (chave is not null)
            valores["Auth:Jwt:Chave"] = chave;

        if (minutosDeValidade is not null)
            valores["Auth:Jwt:MinutosDeValidade"] = minutosDeValidade.Value.ToString();

        if (emissor is not null)
            valores["Auth:Jwt:Emissor"] = emissor;

        if (audiencia is not null)
            valores["Auth:Jwt:Audiencia"] = audiencia;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }

    [Fact]
    public void Ler_RecusaChaveAusente()
    {
        IConfiguration configuracao = CriarConfiguracao(chave: null);

        InvalidOperationException erro = Assert.Throws<InvalidOperationException>(
            () => JwtConfiguracao.Ler(configuracao));

        Assert.Contains("Auth:Jwt:Chave", erro.Message);
        Assert.Contains("Auth__Jwt__Chave", erro.Message);
    }

    [Fact]
    public void Ler_RecusaChaveMenorQueOTamanhoMinimo()
    {
        // 31 bytes: um byte abaixo do mínimo exigido.
        string chaveCurta = new string('a', JwtConfiguracao.TamanhoMinimoChaveEmBytes - 1);

        Assert.Throws<InvalidOperationException>(
            () => JwtConfiguracao.Ler(CriarConfiguracao(chave: chaveCurta)));
    }

    [Fact]
    public void Ler_AceitaChaveNoTamanhoMinimo()
    {
        string chaveNoLimite = new string('k', JwtConfiguracao.TamanhoMinimoChaveEmBytes);

        JwtConfiguracao configuracao = JwtConfiguracao.Ler(CriarConfiguracao(chave: chaveNoLimite));

        Assert.Equal(
            JwtConfiguracao.TamanhoMinimoChaveEmBytes,
            Encoding.UTF8.GetBytes(chaveNoLimite).Length);
        Assert.Equal(256, configuracao.ChaveDeAssinatura.KeySize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Ler_RecusaValidadeNaoPositiva(int minutos)
    {
        Assert.Throws<InvalidOperationException>(
            () => JwtConfiguracao.Ler(CriarConfiguracao(minutosDeValidade: minutos)));
    }

    [Fact]
    public void Ler_UsaPadroesQuandoEmissorEAudienciaNaoSaoInformados()
    {
        JwtConfiguracao configuracao = JwtConfiguracao.Ler(CriarConfiguracao());

        Assert.Equal("Portfy.Api", configuracao.Emissor);
        Assert.Equal("Portfy.Spa", configuracao.Audiencia);
        Assert.Equal(60, configuracao.MinutosDeValidade);
    }

    [Fact]
    public void Ler_LeEmissorAudienciaEValidadeConfigurados()
    {
        JwtConfiguracao configuracao = JwtConfiguracao.Ler(CriarConfiguracao(
            minutosDeValidade: 15,
            emissor: "Portfy.Teste",
            audiencia: "Portfy.Teste.Spa"));

        Assert.Equal("Portfy.Teste", configuracao.Emissor);
        Assert.Equal("Portfy.Teste.Spa", configuracao.Audiencia);
        Assert.Equal(15, configuracao.MinutosDeValidade);
    }

    [Fact]
    public void Ler_RejeitaHashQueNaoEhBcrypt()
    {
        // Reforço da fronteira de credencial: hash fora do formato BCrypt é
        // tratado como inválido, nunca como senha em texto puro.
        var hasher = new SenhaHasher(fatorDeTrabalho: 10);

        Assert.False(hasher.Verificar("senha", "senha-em-texto-puro"));
        Assert.False(hasher.Verificar("senha", string.Empty));
    }

    [Fact]
    public void SenhaHasher_RecusaFatorDeTrabalhoForaDaFaixa()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SenhaHasher(SenhaHasher.FatorDeTrabalhoMinimo - 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SenhaHasher(SenhaHasher.FatorDeTrabalhoMaximo + 1));
    }
}
