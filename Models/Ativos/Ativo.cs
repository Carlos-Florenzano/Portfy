namespace Portfy.Models;

/// Classe base abstrata para todos os ativos negociáveis
public abstract class Ativo
{ // Aqui há o protected set para conceder flexibilidade para que uma subclasse específica ajuste internamente
//  o estado herdado caso uma regra de negócio particular exija isso, mantendo o dado blindado contra alterações
//  externas descontroladas.
    public string Ticker { get; protected set; }
    public string Nome { get; protected set; }
    public decimal PrecoAtual { get; protected set; }
    public decimal RentabilidadeAcumulada { get; protected set; }
    public abstract TipoAtivo Tipo { get; }

    protected Ativo(string ticker, string nome, decimal precoInicial)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("O ticker não pode ser vazio.", nameof(ticker));

        if (precoInicial <= 0)
            throw new ArgumentException("O preço inicial deve ser maior que zero.", nameof(precoInicial));

        Ticker = ticker.ToUpper();
        Nome = nome;
        PrecoAtual = precoInicial;
        RentabilidadeAcumulada = 0m;
    }

    public void AtualizarPreco(decimal novoPreco)
    {
        if (novoPreco <= 0)
            throw new ArgumentException($"O preço do ativo '{Ticker}' deve ser maior que zero.", nameof(novoPreco));

        decimal precoAntigo = PrecoAtual;
        PrecoAtual = Math.Round(novoPreco, 2);

        if (precoAntigo > 0)
        {
            RentabilidadeAcumulada = Math.Round(((PrecoAtual - precoAntigo) / precoAntigo) * 100, 2);
        }
    }

    // Método abstrato: cada ativo calcula seus rendimentos/proventos de forma específica (Polimorfismo)
    public abstract decimal CalcularRendimento(int quantidade, decimal taxaOuParametro);
}

