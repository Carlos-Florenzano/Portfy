// Representa um ativo mantido em carteira, com sua quantidade e preço médio ponderado.
// Comentarei posteriormente sobre ele, mas o objetivo é de aumentar a coesão.

/*  namespace Portfy.Models;

public class Posicao
{
    public Ativo Ativo { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }

    public Posicao(Ativo ativo, int quantidade, decimal precoMedio)
    {
        Ativo = ativo ?? throw new ArgumentNullException(nameof(ativo));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade da posição deve ser maior que zero.", nameof(quantidade));

        if (precoMedio <= 0)
            throw new ArgumentException("O preço médio deve ser um valor positivo.", nameof(precoMedio));

        Quantidade = quantidade;
        PrecoMedio = precoMedio;
    }

    public void AtualizarPrecoMedioEQuantidade(int quantidadeAdicional, decimal precoAtual)
    {
        if (quantidadeAdicional <= 0)
            throw new ArgumentException("A quantidade adicional deve ser maior que zero.", nameof(quantidadeAdicional));

        if (precoAtual <= 0)
            throw new ArgumentException("O preço de aquisição deve ser maior que zero.", nameof(precoAtual));

        decimal custoTotalAtual = Quantidade * PrecoMedio;
        decimal custoNovasAquisicoes = quantidadeAdicional * precoAtual;

        Quantidade += quantidadeAdicional;
        PrecoMedio = Math.Round((custoTotalAtual + custoNovasAquisicoes) / Quantidade, 2);
    }

    public void ReduzirQuantidade(int quantidadeRemovida)
    {
        if (quantidadeRemovida <= 0)
            throw new ArgumentException("A quantidade removida deve ser maior que zero.", nameof(quantidadeRemovida));

        if (quantidadeRemovida > Quantidade)
            throw new InvalidOperationException("Não é possível remover mais unidades do que as disponíveis na posição.");

        Quantidade -= quantidadeRemovida;
    }

    public decimal ObterValorTotalAtual() => Quantidade * Ativo.PrecoAtual;

    public decimal ObterRendimentoTotalEstimado(decimal parametroAdicional = 0m)
    {
        // Polimorfismo em ação: chama o método específico da classe filha concreta
        return Ativo.CalcularRendimento(Quantidade, parametroAdicional);
    }
}
*/