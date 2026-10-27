// Dividir Acao, FundoImobiliario, para fazer a abstração e o polimorfismo
namespace Portfy.Models;

/// Ação: Gera proventos via Dividend Yield estimado
public class Acao : Ativo
{
    public decimal DividendYieldAnual { get; private set; }
    public override TipoAtivo Tipo => TipoAtivo.Acao;

//  O dividendYield é um indicador financeiro que mede a taxa de retorno de uma ação ou fundo imobiliário (FII)
    public Acao(string ticker, string nome, decimal precoInicial, decimal dividendYieldAnual)
        : base(ticker, nome, precoInicial)
    {
        DividendYieldAnual = dividendYieldAnual >= 0 ? dividendYieldAnual : 0m;
    }

    public override decimal CalcularRendimento(int quantidade, decimal taxaOuParametro)
    {
        // Rendimento periódico estimado com base no DY e na cotação atual
        decimal valorInvestido = PrecoAtual * quantidade;
        return Math.Round(valorInvestido * (DividendYieldAnual / 100m) / 12m, 2);
    }
}