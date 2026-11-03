using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Cria aportes simulados persistidos no SQLite. O aporte pertence à carteira do
/// usuário autenticado: o saldo e o histórico mudam na mesma transação, então o
/// frontend nunca vê um aporte gravado sem o saldo correspondente (arch §9.1).
/// Registrado como Scoped porque depende do AppDbContext.
public sealed class AporteService : IAporteService
{
    private readonly AppDbContext _db;
    private readonly ICarteiraService _carteiraService;

    public AporteService(AppDbContext db, ICarteiraService carteiraService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _carteiraService = carteiraService ?? throw new ArgumentNullException(nameof(carteiraService));
    }

    public AporteRealizado Criar(int usuarioId, decimal valorAportado, int? carteiraId = null)
    {
        CarteiraSimulada carteira = _carteiraService.ObterCarteira(usuarioId);

        // A carteira é sempre a do usuário do token. Um id explícito só é aceito
        // quando coincide com ela; qualquer outro id é 404, nunca acesso cruzado.
        if (carteiraId is > 0 && carteira.Id != carteiraId)
            throw new KeyNotFoundException("Carteira não encontrada para o usuário autenticado.");

        // Id 0 = sem identidade: a chave é gerada pelo banco (arch §9.4).
        var aporte = new AporteSimulado(0, valorAportado, usuarioId, carteira.Id);

        // AdicionarAporte soma ao saldo e adiciona à coleção da carteira; o Add
        // explícito garante que o EF grave a linha mesmo sem DetectChanges.
        carteira.AdicionarAporte(aporte);
        _db.AportesSimulados.Add(aporte);

        _db.SaveChanges();

        return new AporteRealizado(
            aporte,
            carteira.SaldoDisponivel,
            carteira.CalcularPatrimonioTotal());
    }

    public HistoricoDeAportes ObterHistorico(int usuarioId, DateOnly? inicio, DateOnly? fim)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("O ID do usuário deve ser um valor positivo.", nameof(usuarioId));

        if (inicio is not null && fim is not null && fim < inicio)
            throw new ArgumentException(
                "A data final do período não pode ser anterior à inicial.",
                nameof(fim));

        // OrigemSalarioId é a identidade do dono do aporte (arch §9.1): o filtro
        // por usuário nunca depende de join nem do id vindo da URL.
        IQueryable<AporteSimulado> consulta = _db.AportesSimulados
            .AsNoTracking()
            .Where(a => a.OrigemSalarioId == usuarioId);

        if (inicio is not null)
        {
            DateTime limiteInferior = inicio.Value.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(a => a.DataAporte >= limiteInferior);
        }

        if (fim is not null)
        {
            // O fim é inclusivo para o usuário: 2026-11-02 inclui o dia inteiro.
            DateTime limiteSuperior = fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(a => a.DataAporte < limiteSuperior);
        }

        List<AporteSimulado> aportes = consulta
            .OrderByDescending(a => a.DataAporte)
            .ThenByDescending(a => a.Id)
            .ToList();

        return new HistoricoDeAportes(aportes, aportes.Sum(a => a.ValorAportado));
    }
}
