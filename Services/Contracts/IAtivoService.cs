using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado do catálogo de ativos e da simulação de variação de preço.
public interface IAtivoService
{
    IReadOnlyCollection<Ativo> ObterTodos();

    Ativo? ObterPorTicker(string ticker);

    /// Cadastra um ativo no catálogo. `tipo` é o nome de TipoAtivo; um valor
    /// desconhecido é recusado como ArgumentException (400).
    Ativo Criar(string ticker, string nome, string tipo, decimal precoAtual);

    VariacaoSimulada? SimularVariacao(string ticker, decimal? percentualCustomizado);

    /// Rendimento mensal estimado de `quantidade` cotas do ativo. A conta é da
    /// implementação do subtipo (`Ativo.CalcularRendimento`); o serviço não replica
    /// a fórmula. Retorna `null` quando o ticker não existe (404 no controller) e
    /// recusa quantidade não positiva como ArgumentException (400).
    RendimentoEstimado? CalcularRendimentoEstimado(string ticker, int quantidade);
}
