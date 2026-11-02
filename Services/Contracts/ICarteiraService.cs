using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Resultado de uma operação de compra ou venda: carrega a carteira já atualizada
/// e os valores usados na resposta, sem que o controller precise recalcular nada.
public sealed record OperacaoCarteira(
    CarteiraSimulada Carteira,
    string Ticker,
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorTotal);

/// Contrato injetado da carteira: o controller depende só desta interface,
/// o que permite testá-lo com um dublê, sem banco e sem estado global.
/// Toda leitura/escrita recebe o usuário do token (arch §9.1).
public interface ICarteiraService
{
    CarteiraSimulada ObterCarteira(int usuarioId);

    OperacaoCarteira? Comprar(int usuarioId, string ticker, int quantidade);

    OperacaoCarteira? Vender(int usuarioId, string ticker, int quantidade);
}
