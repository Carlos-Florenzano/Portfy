using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Portfy.Data;

namespace Portfy.Tests.Data;

/// Regressão da migration PosseDaCarteira em um banco que já existe.
///
/// O teste cria o schema antigo (apenas InitialCreate), insere dados no formato
/// anterior (carteira/orçamento globais e aportes/posições sem carteira) e então
/// aplica a migration nova. É o cenário que não pode falhar em uma instalação
/// que já tem usuários e dados financeiros.
public class MigracaoPosseTests : IDisposable
{
    private const string MigracaoInicial = "20261101003441_InitialCreate";

    private readonly SqliteConnection _conexao;

    public MigracaoPosseTests()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        SqlitePragmaInterceptor.Aplicar(_conexao);
    }

    public void Dispose() => _conexao.Dispose();

    private AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);

    private static void InserirUsuario(AppDbContext contexto, string login) =>
        contexto.Database.ExecuteSqlRaw(
            "INSERT INTO Usuarios (Nome, SalarioMensal, DataRecebimento, Login, SenhaHash, ContaVerificada) " +
            "VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            "Usuário Antigo",
            "3000.0",
            "2026-01-01 00:00:00",
            login,
            "hash-antigo",
            1);

    [Fact]
    public void Migration_AssociaDadosAntigosAosUsuariosECriaOsQueFaltam()
    {
        using (AppDbContext contexto = NovoContexto())
        {
            // Banco no formato anterior: só a InitialCreate aplicada.
            contexto.GetService<IMigrator>().Migrate(MigracaoInicial);

            InserirUsuario(contexto, "ana.old@portfy.test");
            InserirUsuario(contexto, "bruno.old@portfy.test");

            // Posição e aportes antigos sem vínculo com carteira (shadow property nula).
            contexto.Database.ExecuteSqlRaw(
                "INSERT INTO Posicoes (AtivoId, Quantidade, PrecoMedio, CarteiraSimuladaId) " +
                "VALUES (1, 10, '38.5', NULL)");

            contexto.Database.ExecuteSqlRaw(
                "INSERT INTO Aportes (DataAporte, ValorAportado, OrigemSalarioId, CarteiraSimuladaId) " +
                "VALUES ('2026-01-02 00:00:00', '500.0', 1, NULL)");

            contexto.Database.ExecuteSqlRaw(
                "INSERT INTO Aportes (DataAporte, ValorAportado, OrigemSalarioId, CarteiraSimuladaId) " +
                "VALUES ('2026-01-02 00:00:00', '700.0', 2, NULL)");
        }

        using (AppDbContext contexto = NovoContexto())
        {
            contexto.Database.Migrate(); // aplica PosseDaCarteira sobre o banco antigo

            var carteiras = contexto.CarteirasSimuladas.OrderBy(c => c.Id).ToList();
            var orcamentos = contexto.Orcamentos.OrderBy(o => o.Id).ToList();
            var posicoes = contexto.Set<Portfy.Models.Posicao>().ToList();
            var aportes = contexto.AportesSimulados.OrderBy(a => a.Id).ToList();

            Assert.Equal(2, carteiras.Count);
            Assert.All(carteiras, c => Assert.Equal("Principal", c.Nome));
            Assert.All(carteiras, c => Assert.Equal(10000m, c.SaldoDisponivel));
            Assert.Equal(new[] { 1, 2 }, carteiras.Select(c => c.UsuarioId).ToArray());

            Assert.Equal(2, orcamentos.Count);
            Assert.All(orcamentos, o => Assert.Equal("Alimentação", o.NomeCategoria));
            Assert.Equal(new[] { 1, 2 }, orcamentos.Select(o => o.UsuarioId).ToArray());

            var posicao = Assert.Single(posicoes);
            Assert.Equal(carteiras[0].Id, posicao.CarteiraId);
            Assert.Equal(10, posicao.Quantidade);

            var mapaCarteira = carteiras.ToDictionary(c => c.UsuarioId, c => c.Id);

            Assert.Equal(2, aportes.Count);
            Assert.All(aportes, a => Assert.Equal(mapaCarteira[a.OrigemSalarioId], a.CarteiraId));
        }
    }

    [Fact]
    public void Migration_SemUsuarios_RemoveOsDadosGlobaisOrfaos()
    {
        using (AppDbContext contexto = NovoContexto())
        {
            contexto.GetService<IMigrator>().Migrate(MigracaoInicial);

            contexto.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
            contexto.Database.ExecuteSqlRaw(
                "INSERT INTO Aportes (DataAporte, ValorAportado, OrigemSalarioId, CarteiraSimuladaId) " +
                "VALUES ('2026-01-02 00:00:00', '500.0', 1, NULL)");
            contexto.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");
        }

        using (AppDbContext contexto = NovoContexto())
        {
            contexto.Database.Migrate();

            Assert.Empty(contexto.CarteirasSimuladas);
            Assert.Empty(contexto.Orcamentos);
            Assert.Empty(contexto.AportesSimulados);
            Assert.Empty(contexto.Set<Portfy.Models.Posicao>());
        }
    }
}
