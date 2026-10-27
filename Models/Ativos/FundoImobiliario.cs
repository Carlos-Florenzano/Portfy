namespace Portfy.Models;

public class FundoImobiliario : Ativo
{
    public decimal RendimentoMensalPorCota { get; private set; }

    public override TipoAtivo Tipo => TipoAtivo.Fii;

    public FundoImobiliario(string ticker, string nome, decimal precoInicial, decimal rendimentoMensalPorCota)
        : base(ticker, nome, precoInicial)
    {
        if (rendimentoMensalPorCota < 0)
            throw new ArgumentException("O rendimento mensal por cota não pode ser negativo.", nameof(rendimentoMensalPorCota));

        RendimentoMensalPorCota = rendimentoMensalPorCota;
    }

    public override decimal CalcularRendimento(int quantidade, decimal parametroAdicional = 0m)
    {
        if (quantidade <= 0) return 0m;

        // FIIs distribuem valor direto por cota mantida
        return Math.Round(quantidade * RendimentoMensalPorCota, 2);
    }
}