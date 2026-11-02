using Portfy.Models;

namespace Portfy.Services.Contracts;

/// Contrato de emissão do token de acesso (ADR-010). O token carrega apenas
/// identidade (id, login, nome) e expiração — nenhuma regra de negócio.
public interface IAuthService
{
    TokenAcesso GerarToken(Usuario usuario);
}
