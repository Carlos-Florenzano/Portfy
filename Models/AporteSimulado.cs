namespace Portfy.Models;

public class AporteSimulado
{
    public int Id { get; private set; }
    public DateTime DataAporte { get; private set; }
    public decimal ValorAportado { get; private set; }

    // O aporte pertence a uma carteira; a carteira pertence ao usuário. As duas
    // chaves são explícitas para que a leitura por usuário não dependa de join implícito.
    public int OrigemSalarioId { get; private set; }
    public int CarteiraId { get; private set; }

    // Construtor exigido pelo EF Core (arch §13.1) para materializar um aporte persistido.
    protected AporteSimulado()
    {
    }

    /// Cria um aporte informado pelo domínio. Id 0 significa "ainda sem identidade":
    /// a chave é gerada pelo banco (arch §9.4), no mesmo padrão já usado em Usuario.
    public AporteSimulado(
        int id,
        decimal valorAportado,
        int origemSalarioId,
        int carteiraId,
        DateTime? dataAporte = null)
    {
        if (id < 0)
            throw new ArgumentException("O ID deve ser positivo.", nameof(id));

        if (valorAportado <= 0)
            throw new ArgumentException("O valor aportado deve ser maior que zero.", nameof(valorAportado));

        if (origemSalarioId <= 0)
            throw new ArgumentException("O ID da origem do salário deve ser válido.", nameof(origemSalarioId));

        if (carteiraId <= 0)
            throw new ArgumentException("O ID da carteira deve ser válido.", nameof(carteiraId));

        Id = id;
        ValorAportado = valorAportado;
        OrigemSalarioId = origemSalarioId;
        CarteiraId = carteiraId;
        DataAporte = dataAporte ?? DateTime.Now;
    }
}
