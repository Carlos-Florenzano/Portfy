using Portfy.Models;
using Portfy.Services;
using Portfy.Services.Contracts;

namespace Portfy.Tests.Services;

/// Prova de que o contrato polimórfico `Ativo.CalcularRendimento` é usado pela
/// aplicação (serviço), e não apenas pelos testes de domínio: a mesma chamada
/// responde diferente conforme o subtipo que está atrás da referência base.
public class RendimentoPolimorficoTests
{
    private sealed class GeradorFixo : IGeradorDeVariacao
    {
        public decimal SortearPercentual(decimal percentualMinimo, decimal percentualMaximo) =>
            percentualMinimo;
    }

    private static AtivoService CriarServico() => new(new GeradorFixo());

    [Fact]
    public void Acao_CalculaPeloDividendYieldMensal()
    {
        RendimentoEstimado? rendimento = CriarServico().CalcularRendimentoEstimado("PETR4", 10);

        Assert.NotNull(rendimento);
        // 38,50 * 10 = 385,00; 12% ao ano / 12 = 3,85
        Assert.Equal(3.85m, rendimento!.Valor);
        Assert.Equal(TipoAtivo.Acao, rendimento.Ativo.Tipo);
    }

    [Fact]
    public void FundoImobiliario_CalculaPeloRendimentoPorCota()
    {
        RendimentoEstimado? rendimento = CriarServico().CalcularRendimentoEstimado("HGLG11", 10);

        Assert.NotNull(rendimento);
        // 10 cotas * 1,10 por cota — fórmula diferente da ação, mesmo contrato.
        Assert.Equal(11.00m, rendimento!.Valor);
        Assert.Equal(TipoAtivo.Fii, rendimento.Ativo.Tipo);
    }

    [Fact]
    public void CriptoSemTaxaDeStaking_DevolveZero()
    {
        RendimentoEstimado? rendimento = CriarServico().CalcularRendimentoEstimado("BTC", 10);

        Assert.NotNull(rendimento);
        Assert.Equal(0m, rendimento!.Valor);
        Assert.Equal(TipoAtivo.Cripto, rendimento.Ativo.Tipo);
    }

    [Fact]
    public void MesmaChamadaSobreReferenciaBase_RespondeDiferentePorSubtipo()
    {
        AtivoService servico = CriarServico();

        decimal acao = servico.CalcularRendimentoEstimado("PETR4", 10)!.Valor;
        decimal fundo = servico.CalcularRendimentoEstimado("HGLG11", 10)!.Valor;
        decimal cripto = servico.CalcularRendimentoEstimado("BTC", 10)!.Valor;

        Assert.NotEqual(acao, fundo);
        Assert.NotEqual(acao, cripto);
        Assert.All(servico.ObterTodos(), ativo =>
            Assert.Equal(
                ativo.CalcularRendimento(10, 0m),
                servico.CalcularRendimentoEstimado(ativo.Ticker, 10)!.Valor));
    }

    [Fact]
    public void QuantidadeNaoPositiva_E400()
    {
        var excecao = Assert.Throws<ArgumentException>(
            () => CriarServico().CalcularRendimentoEstimado("PETR4", 0));

        Assert.Equal("quantidade", excecao.ParamName);
    }

    [Fact]
    public void TickerInexistente_DevolveNull()
    {
        Assert.Null(CriarServico().CalcularRendimentoEstimado("NAOEXISTE", 10));
    }
}
