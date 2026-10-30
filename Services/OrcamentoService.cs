using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Orçamento de exemplo mantido no processo. Registrado como Singleton
/// até a persistência em SQLite assumir o estado (dívida P0-a).
public sealed class OrcamentoService : IOrcamentoService
{
    private readonly Orcamento _orcamento = new("Alimentação", 1200m, 24m);

    public Orcamento ObterOrcamento() => _orcamento;

    public void AdicionarDespesa(decimal valor) => _orcamento.AdicionarDespesa(valor);
}
