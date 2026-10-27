namespace Portfy.Models;

public class RendaFixa : Ativo
{
    public decimal TaxaAnualContratada { get; private set; }

    public override TipoAtivo Tipo => TipoAtivo.RendaFixa;

    public RendaFixa(string ticker, string nome, decimal precoInicial, decimal taxaAnualContratada)
        : base(ticker, nome, precoInicial)
    {
        if (taxaAnualContratada < 0)
            throw new ArgumentException("A taxa anual contratada não pode ser negativa.", nameof(taxaAnualContratada));

        TaxaAnualContratada = taxaAnualContratada;
    }

    public override decimal CalcularRendimento(int quantidade, decimal parametroAdicional = 0m)
    {
        if (quantidade <= 0) return 0m;

        decimal valorInvestido = PrecoAtual * quantidade;
        decimal taxaMensal = (TaxaAnualContratada / 100m) / 12m;
        return Math.Round(valorInvestido * taxaMensal, 2);
    }
}