using System;

namespace Portfy.Dominio.Investimentos;

public enum TipoOrdemInvestimento
{
    Compra,
    Venda
}

public enum StatusOrdemInvestimento
{
    Executada,
    Rejeitada
}

public sealed class OrdemInvestimento
{
    public string Codigo { get; }
    public DateTime DataHora { get; }
    public TipoOrdemInvestimento Tipo { get; }
    public string Ticker { get; }
    public string NomeAtivo { get; }
    public int Quantidade { get; }
    public decimal PrecoUnitario { get; }
    public decimal ValorTotal { get; }
    public StatusOrdemInvestimento Status { get; }
    public string? MotivoRejeicao { get; }

    internal OrdemInvestimento(
        string codigo,
        DateTime dataHora,
        TipoOrdemInvestimento tipo,
        Ativo ativo,
        int quantidade,
        decimal precoUnitario,
        StatusOrdemInvestimento status,
        string? motivoRejeicao)
    {
        Codigo = codigo;
        DataHora = dataHora;
        Tipo = tipo;
        Ticker = ativo.Ticker;
        NomeAtivo = ativo.Nome;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        ValorTotal = precoUnitario * quantidade;
        Status = status;
        MotivoRejeicao = motivoRejeicao;
    }
}
