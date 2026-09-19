using System;
using System.Collections.Generic;
using Portfy.ModuloInvestimentos; //já importa o namespace do Ativo.cs, então não precisa importar novamente.

public class CarteiraSimulada
{
    public CarteiraSimulada() {}// Construtor padrão, caso queira criar uma carteira sem saldo inicial.

    public CarteiraSimulada(int saldo)
    {
        SaldoDisponivel = saldo;
    }

    public int SaldoDisponivel { get; set; }

    public List<Posicao> ListaPosicoes { get; set; } = new List<Posicao>();// Inicializa a lista de posições como uma nova lista vazia, garantindo que não seja nula.

    public void ComprarAtivo(Ativo ativo, int quantidade)
    {
        decimal valorCompra = ativo.PrecoAtual * quantidade;

        if (valorCompra > SaldoDisponivel)
        {
            Console.WriteLine("Saldo insuficiente.");
            return;
        }

        Posicao? posicao = ListaPosicoes.Find(p => p.Ativo == ativo); //procura na lista de posições se já existe uma posição para o ativo que está sendo comprado. Se encontrar, retorna a posição; caso contrário, retorna null.

        if (posicao == null)
        {
            ListaPosicoes.Add(
                new Posicao(ativo, quantidade, ativo.PrecoAtual)
            );
        }
        else
        {
            decimal valorTotal =
                (posicao.Quantidade * posicao.PrecoMedio) + valorCompra;//Atualiza a quantidade.

            posicao.Quantidade += quantidade;
            posicao.PrecoMedio = valorTotal / posicao.Quantidade;//Calcula o preço médio da posição existente.
        }

        SaldoDisponivel -= (int)valorCompra;
    }

    public void VenderAtivo(Ativo ativo, int quantidade)
    {
        Posicao? posicao = ListaPosicoes.Find(p => p.Ativo == ativo);

        if (posicao == null || posicao.Quantidade < quantidade)
        {
            Console.WriteLine("Quantidade insuficiente para venda.");
            return;
        }

        decimal valorVenda = ativo.PrecoAtual * quantidade;//calcula o valor da venda.

        posicao.Quantidade -= quantidade;//diminui a quantidade da posição existente.
        SaldoDisponivel += (int)valorVenda;//devolve o dinheiro da venda para o saldo disponível.

        if (posicao.Quantidade == 0)
        {
            ListaPosicoes.Remove(posicao);
        }
    }

    public decimal CalcularPatrimonioTotal()
    {
        decimal patrimonio = SaldoDisponivel;

        foreach (Posicao posicao in ListaPosicoes)
        {
            patrimonio += posicao.Quantidade * posicao.PrecoMedio;
        }

        return patrimonio;
    }
}


public class Posicao
{
    public Ativo Ativo { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }

    public Posicao(Ativo ativo, int quantidade, decimal precoMedio)
    {
        Ativo = ativo;
        Quantidade = quantidade;
        PrecoMedio = precoMedio;
    }
}