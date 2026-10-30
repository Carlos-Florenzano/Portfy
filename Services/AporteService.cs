using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Cria aportes simulados. O contador em memória é provisório: a geração de Id
/// passa a ser do banco quando a persistência entrar (dívida P0-a).
public sealed class AporteService : IAporteService
{
    private int _proximoId = 1;

    public AporteSimulado Criar(decimal valorAportado, int origemSalarioId) =>
        new(_proximoId++, valorAportado, origemSalarioId);
}
