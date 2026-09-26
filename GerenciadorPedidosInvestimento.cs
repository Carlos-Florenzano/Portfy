using System;
using System.Collections.Generic;
using System.Linq;

namespace Portfy.Dominio.Investimentos;

public sealed class GerenciadorPedidosInvestimento
{
    private readonly object _sincronizacao = new();
    private readonly List<OrdemInvestimento> _ordens = new();
    private int _proximoNumero = 1;

    public IReadOnlyList<OrdemInvestimento> Listar()
    {
        lock (_sincronizacao)
        {
            return _ordens.ToArray();
        }
    }

    public OrdemInvestimento Comprar(CarteiraSimulada carteira, Ativo ativo, int quantidade)
    {
        return Executar(carteira, ativo, quantidade, TipoOrdemInvestimento.Compra);
    }

    public OrdemInvestimento Vender(CarteiraSimulada carteira, Ativo ativo, int quantidade)
    {
        return Executar(carteira, ativo, quantidade, TipoOrdemInvestimento.Venda);
    }

    private OrdemInvestimento Executar(
        CarteiraSimulada carteira,
        Ativo ativo,
        int quantidade,
        TipoOrdemInvestimento tipo)
    {
        ArgumentNullException.ThrowIfNull(carteira);
        ArgumentNullException.ThrowIfNull(ativo);

        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade deve ser maior que zero.");

        lock (_sincronizacao)
        {
            string codigo = $"ORD-{_proximoNumero++:D6}";
            decimal precoUnitario = ativo.PrecoAtual;
            string? motivoRejeicao = null;

            try
            {
                if (tipo == TipoOrdemInvestimento.Compra)
                    carteira.ComprarAtivo(ativo, quantidade);
                else
                    carteira.VenderAtivo(ativo, quantidade);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                motivoRejeicao = ex.Message;
            }

            var ordem = new OrdemInvestimento(
                codigo,
                DateTime.Now,
                tipo,
                ativo,
                quantidade,
                precoUnitario,
                motivoRejeicao is null ? StatusOrdemInvestimento.Executada : StatusOrdemInvestimento.Rejeitada,
                motivoRejeicao);

            _ordens.Insert(0, ordem);
            return ordem;
        }
    }
}
