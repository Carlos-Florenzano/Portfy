namespace Portfy.Models;

public class Cripto : Ativo
{
    public decimal TaxaStakingAnual { get; private set; }

    public override TipoAtivo Tipo => TipoAtivo.Cripto;

    public Cripto(string ticker, string nome, decimal precoInicial, decimal taxaStakingAnual = 0m)
        : base(ticker, nome, precoInicial)
    {
        if (taxaStakingAnual < 0)
            throw new ArgumentException("A taxa de staking não pode ser negativa.", nameof(taxaStakingAnual));

        TaxaStakingAnual = taxaStakingAnual;
    }

    public override decimal CalcularRendimento(int quantidade, decimal parametroAdicional = 0m)
    {
        if (quantidade <= 0 || TaxaStakingAnual <= 0) return 0m;

        decimal valorInvestido = PrecoAtual * quantidade;
        decimal taxaMensal = (TaxaStakingAnual / 100m) / 12m;
        return Math.Round(valorInvestido * taxaMensal, 2);
    }
}