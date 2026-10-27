namespace Portfy.Models;

public class Etf : Ativo
{
    public string IndiceReferencia { get; private set; }
    public decimal TaxaAdministracaoAnual { get; private set; }

    public override TipoAtivo Tipo => TipoAtivo.Etf;

    public Etf(string ticker, string nome, decimal precoInicial, string indiceReferencia, decimal taxaAdministracaoAnual)
        : base(ticker, nome, precoInicial)
    {
        if (string.IsNullOrWhiteSpace(indiceReferencia))
            throw new ArgumentException("O índice de referência não pode ser vazio.", nameof(indiceReferencia));

        if (taxaAdministracaoAnual < 0)
            throw new ArgumentException("A taxa de administração não pode ser negativa.", nameof(taxaAdministracaoAnual));

        IndiceReferencia = indiceReferencia;
        TaxaAdministracaoAnual = taxaAdministracaoAnual;
    }

    public override decimal CalcularRendimento(int quantidade, decimal variacaoIndiceMensal = 0m)
    {
        if (quantidade <= 0) return 0m;

        decimal valorInvestido = PrecoAtual * quantidade;
        decimal taxaAdminMensal = (TaxaAdministracaoAnual / 100m) / 12m;
        decimal taxaLiquidaMensal = (variacaoIndiceMensal / 100m) - taxaAdminMensal;

        return Math.Round(valorInvestido * taxaLiquidaMensal, 2);
    }
}