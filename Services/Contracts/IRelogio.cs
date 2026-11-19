namespace Portfy.Services.Contracts;

/// Abstração de tempo (arch §16.1, item 7): o domínio e os serviços recebem o
/// instante por injeção, e o teste controla o relógio sem espera real.
public interface IRelogio
{
    DateTimeOffset UtcNow { get; }
}
