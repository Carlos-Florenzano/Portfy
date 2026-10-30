using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado do catálogo de ativos e da simulação de variação de preço.
public interface IAtivoService
{
    IReadOnlyCollection<Ativo> ObterTodos();

    Ativo? ObterPorTicker(string ticker);

    VariacaoSimulada? SimularVariacao(string ticker, decimal? percentualCustomizado);
}
