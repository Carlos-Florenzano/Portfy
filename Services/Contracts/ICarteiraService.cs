using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato injetado da carteira: o controller depende só desta interface,
/// o que permite testá-lo com um dublê, sem banco e sem estado global.
public interface ICarteiraService
{
    CarteiraSimulada ObterCarteira();
}
