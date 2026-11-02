using Portfy.Dtos.Ativo;

namespace Portfy.Dtos.Carteira;

/// Projeção da posição: `Ativo` continua aninhado, igual ao contrato anterior.
public sealed class PosicaoResponse
{
    public int Id { get; init; }
    public AtivoResponse Ativo { get; init; } = new();
    public int Quantidade { get; init; }
    public decimal PrecoMedio { get; init; }
}
