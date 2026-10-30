using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado de criação de aportes (a geração do Id é responsabilidade do serviço).
public interface IAporteService
{
    AporteSimulado Criar(decimal valorAportado, int origemSalarioId);
}
