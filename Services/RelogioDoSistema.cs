using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Implementação de produção de IRelogio: o único ponto autorizado a ler o
/// relógio do sistema.
public sealed class RelogioDoSistema : IRelogio
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
