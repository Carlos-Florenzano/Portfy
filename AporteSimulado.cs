using System;
using System.Collections.Generic;
using System.Linq;

namespace Portfy.Dominio.Investimentos;

public class AporteSimulado
{
    public int Id { get; private set; }
    public DateTime DataAporte { get; private set; }
    public decimal ValorAportado { get; private set; }
    public int OrigemSalarioId { get; private set; }

    public AporteSimulado(int id, decimal valorAportado, int origemSalarioId, DateTime? dataAporte = null)
    {
        if (id <= 0)
            throw new ArgumentException("O ID deve ser positivo.", nameof(id));

        if (valorAportado <= 0)
            throw new ArgumentException("O valor aportado deve ser maior que zero.", nameof(valorAportado));

        if (origemSalarioId <= 0)
            throw new ArgumentException("O ID da origem do salário deve ser válido.", nameof(origemSalarioId));

        Id = id;
        ValorAportado = valorAportado;
        OrigemSalarioId = origemSalarioId;
        DataAporte = dataAporte ?? DateTime.Now;
    }
}
