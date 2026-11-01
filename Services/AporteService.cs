using Portfy.Data;
using Portfy.Models;
using Portfy.Services.Contracts;

namespace Portfy.Services;

/// Cria aportes simulados persistidos no SQLite. O Id passou a ser gerado pelo banco
/// (arch §9.4) e o aporte mantém integridade referencial com o usuário dono do
/// salário de origem. Registrado como Scoped porque depende do AppDbContext.
public sealed class AporteService : IAporteService
{
    private readonly AppDbContext _db;

    public AporteService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public AporteSimulado Criar(decimal valorAportado, int origemSalarioId)
    {
        // Integridade referencial verificada antes da escrita, com mensagem de domínio
        // em vez de uma falha de chave estrangeira no banco.
        if (!_db.Usuarios.Any(u => u.Id == origemSalarioId))
            throw new ArgumentException(
                "O usuário de origem do salário não existe.",
                nameof(origemSalarioId));

        // Id 0 = sem identidade: a chave é gerada pelo banco (arch §9.4).
        var aporte = new AporteSimulado(0, valorAportado, origemSalarioId);

        _db.AportesSimulados.Add(aporte);
        _db.SaveChanges();

        return aporte;
    }
}
