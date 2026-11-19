namespace Portfy.Dtos.Relatorio;

/// Relatório consolidado do usuário autenticado.
///
/// O período filtra os fluxos (aportes). Saldos, posições e orçamentos são o
/// retrato atual do estado persistido — o sistema não guarda histórico de saldo,
/// e inventar essa série aqui seria um dado falso.
public sealed class RelatorioResponse
{
    public string NomeUsuario { get; init; } = string.Empty;

    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }

    public decimal SalarioMensal { get; init; }
    public decimal SaldoDisponivel { get; init; }
    public decimal PatrimonioTotal { get; init; }
    public decimal TotalInvestido { get; init; }

    public decimal TotalAportado { get; init; }
    public int QuantidadeAportes { get; init; }

    public decimal TotalOrcado { get; init; }
    public decimal TotalGasto { get; init; }
    public decimal SaldoOrcamentos { get; init; }
    public decimal RendaDisponivel { get; init; }

    public IReadOnlyCollection<AporteRelatorioResponse> Aportes { get; init; } =
        Array.Empty<AporteRelatorioResponse>();

    public IReadOnlyCollection<PosicaoRelatorioResponse> Posicoes { get; init; } =
        Array.Empty<PosicaoRelatorioResponse>();

    public IReadOnlyCollection<OrcamentoRelatorioResponse> Orcamentos { get; init; } =
        Array.Empty<OrcamentoRelatorioResponse>();
}
