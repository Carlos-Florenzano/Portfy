using System;
using System.Collections.Generic;
using System.Linq;
namespace Portfy.Dominio.Investimentos;

public class CarteiraSimulada
{
    // Objeto de sincronização para garantir Thread Safety em ambientes multithread
    private readonly object _lock = new();

    // Propriedades com private set para integridade dos dados
    public decimal SaldoDisponivel { get; private set; }

    // Listas internas privadas para proteger o estado do objeto
    private readonly List<Posicao> _posicoes = new();
    private readonly List<AporteSimulado> _aportes = new();

    // Propriedades públicas apenas para leitura (Exposição segura de coleções)
    public IReadOnlyCollection<Posicao> Posicoes
    {
        get
        {
            lock (_lock)
            {
                return _posicoes.AsReadOnly();
            }
        }
    }

    public IReadOnlyCollection<AporteSimulado> Aportes
    {
        get
        {
            lock (_lock)
            {
                return _aportes.AsReadOnly();
            }
        }
    }

    // Construtores
    public CarteiraSimulada()
    {
        SaldoDisponivel = 0m;
    }

    public CarteiraSimulada(decimal saldoInicial)
    {
        if (saldoInicial < 0)
            throw new ArgumentException("O saldo inicial não pode ser negativo.", nameof(saldoInicial));

        SaldoDisponivel = saldoInicial;
    }

    // Método para receber aportes com proteção de concorrência
    public void AdicionarAporte(AporteSimulado aporte)
    {
        if (aporte == null)
            throw new ArgumentNullException(nameof(aporte));

        lock (_lock)
        {
            _aportes.Add(aporte);
            SaldoDisponivel += aporte.ValorAportado;
        }
    }

    // Método de compra com thread safety (para evitar que, ao pedir mais de uma vez, que caso cada processo seja feito em uma thread, que evite de corromper a lista)
    public void ComprarAtivo(Ativo ativo, int quantidade)
    {
        if (ativo == null)
            throw new ArgumentNullException(nameof(ativo));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade para compra deve ser maior que zero.", nameof(quantidade));

        lock (_lock)
        {
            decimal valorTotalCompra = ativo.PrecoAtual * quantidade;

            if (valorTotalCompra > SaldoDisponivel)
                throw new InvalidOperationException($"Saldo insuficiente para realizar a compra. Saldo atual: R$ {SaldoDisponivel:F2}.");

            Posicao? posicao = _posicoes.FirstOrDefault(p => p.Ativo == ativo);

            if (posicao == null)
            {
                _posicoes.Add(new Posicao(ativo, quantidade, ativo.PrecoAtual));
            }
            else
            {
                posicao.AtualizarPrecoMedioEQuantidade(quantidade, ativo.PrecoAtual);
            }

            SaldoDisponivel -= valorTotalCompra;
        }
    }

    // Método de venda com thread safety (para resolver o problema similar ao do método de compra com thread safety)
    public void VenderAtivo(Ativo ativo, int quantidade)
    {
        if (ativo == null)
            throw new ArgumentNullException(nameof(ativo));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade para venda deve ser maior que zero.", nameof(quantidade));

        lock (_lock)
        {
            Posicao? posicao = _posicoes.FirstOrDefault(p => p.Ativo == ativo);

            if (posicao == null || posicao.Quantidade < quantidade)
                throw new InvalidOperationException("Quantidade de ativos insuficiente em carteira para realizar a venda.");

            decimal valorVenda = ativo.PrecoAtual * quantidade;

            posicao.ReduzirQuantidade(quantidade);
            SaldoDisponivel += valorVenda;

            if (posicao.Quantidade == 0)
            {
                _posicoes.Remove(posicao);
            }
        }
    }

    public decimal CalcularPatrimonioTotal()
    {
        lock (_lock)
        {
            // Patrimônio Total = Saldo em Dinheiro + Valor Atual das Posições
            decimal valorTotalEmAtivos = _posicoes.Sum(p => p.Quantidade * p.Ativo.PrecoAtual);
            return SaldoDisponivel + valorTotalEmAtivos;
        }
    }
}

// Classe Posicao imutável e protegida
public class Posicao
{
    public Ativo Ativo { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }

    public Posicao(Ativo ativo, int quantidade, decimal precoMedio)
    {
        if (ativo == null)
            throw new ArgumentNullException(nameof(ativo));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade da posição deve ser maior que zero.", nameof(quantidade));

        if (precoMedio <= 0)
            throw new ArgumentException("O preço médio deve ser um valor positivo.", nameof(precoMedio));

        Ativo = ativo;
        Quantidade = quantidade;
        PrecoMedio = precoMedio;
    }

    public void AtualizarPrecoMedioEQuantidade(int quantidadeAdicional, decimal precoAtual)
    {
        decimal custoTotalAtual = Quantidade * PrecoMedio;
        decimal custoNovasAquisicoes = quantidadeAdicional * precoAtual;

        Quantidade += quantidadeAdicional;
        PrecoMedio = (custoTotalAtual + custoNovasAquisicoes) / Quantidade;
    }

    public void ReduzirQuantidade(int quantidadeRemovida)
    {
        if (quantidadeRemovida > Quantidade)
            throw new InvalidOperationException("Não é possível remover mais unidades do que as disponíveis na posição.");

        Quantidade -= quantidadeRemovida;
    }
}
