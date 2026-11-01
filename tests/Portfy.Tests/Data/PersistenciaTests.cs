using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Services;
using Portfy.Services.Contracts;

namespace Portfy.Tests.Data;

/// Prova de persistência real: cada teste usa um AppDbContext novo sobre o MESMO banco
/// SQLite em memória (equivalente a reiniciar a API). Se algum serviço voltasse a guardar
/// estado em coleção C#, estas asserções falhariam na segunda leitura.
public class PersistenciaTests : IDisposable
{
    private readonly SqliteConnection _conexao;

    public PersistenciaTests()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();

        using AppDbContext contexto = NovoContexto();
        contexto.Database.Migrate(); // mesmo esquema e seed do portfy.db, criados por migration
    }

    public void Dispose() => _conexao.Dispose();

    private AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);

    private sealed class GeradorFixo : IGeradorDeVariacao
    {
        public decimal SortearPercentual(decimal percentualMinimo, decimal percentualMaximo) =>
            percentualMinimo;
    }

    [Fact]
    public void UsuarioCadastrado_SobreviveATrocaDeContexto()
    {
        int id;

        using (AppDbContext contexto = NovoContexto())
        {
            id = new UsuarioService(contexto)
                .Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123")
                .Id;
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var usuario = new UsuarioService(contexto)
                .Autenticar("ANA@PORTFY.TEST", "segredo123");

            Assert.NotNull(usuario);
            Assert.Equal(id, usuario!.Id);
        }
    }

    [Fact]
    public void SeedDoCatalogo_NaoDuplicaNemSobrescrevePrecoSimulado()
    {
        using (AppDbContext contexto = NovoContexto())
        {
            var service = new AtivoService(contexto, new GeradorFixo());

            Assert.Equal(4, service.ObterTodos().Count);
            service.SimularVariacao("PETR4", 10m); // 38,50 -> 42,35
        }

        using (AppDbContext contexto = NovoContexto())
        {
            contexto.Database.Migrate(); // reinício: nada pendente, o seed não roda de novo

            var service = new AtivoService(contexto, new GeradorFixo());

            Assert.Equal(4, service.ObterTodos().Count);
            Assert.Equal(42.35m, service.ObterPorTicker("PETR4")!.PrecoAtual);
            Assert.Equal(62.10m, service.ObterPorTicker("VALE3")!.PrecoAtual);
        }
    }

    [Fact]
    public void ValoresMonetarios_PreservamPrecisaoAposIdaEVolta()
    {
        using (AppDbContext contexto = NovoContexto())
        {
            new UsuarioService(contexto).Cadastrar("Bruno", 1234.56m, "bruno@portfy.test", "segredo456");
            new UsuarioService(contexto).Cadastrar("Carla", 5678.987654m, "carla@portfy.test", "segredo789");
        }

        using (AppDbContext contexto = NovoContexto())
        {
            Assert.Equal(
                1234.56m,
                contexto.Usuarios.Single(u => u.Login == "bruno@portfy.test").SalarioMensal);

            // Sem truncamento: o SQLite guarda decimal como TEXT, com todos os dígitos.
            Assert.Equal(
                5678.987654m,
                contexto.Usuarios.Single(u => u.Login == "carla@portfy.test").SalarioMensal);
        }
    }

    [Fact]
    public void PosicoesDaCarteira_SaoPersistidasERecarregadas()
    {
        using (AppDbContext contexto = NovoContexto())
        {
            var carteira = new CarteiraService(contexto).ObterCarteira();
            var petr4 = contexto.Ativos.Single(a => a.Ticker == "PETR4");

            carteira.ComprarAtivo(petr4, 10);
            contexto.SaveChanges();
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var carteira = new CarteiraService(contexto).ObterCarteira();
            var posicao = Assert.Single(carteira.Posicoes);

            Assert.Equal(10, posicao.Quantidade);
            Assert.Equal(38.50m, posicao.PrecoMedio);
            Assert.Equal("PETR4", posicao.Ativo.Ticker);
            Assert.Equal(9615.00m, carteira.SaldoDisponivel);
        }
    }

    [Fact]
    public void Aporte_ExigeUsuarioExistenteEGeraIdNoBanco()
    {
        int usuarioId;

        using (AppDbContext contexto = NovoContexto())
        {
            usuarioId = new UsuarioService(contexto)
                .Cadastrar("Ana", 3000m, "ana@portfy.test", "segredo123")
                .Id;

            var aportes = new AporteService(contexto);

            // Integridade referencial: origem do salário inexistente é recusada antes da escrita.
            Assert.Throws<ArgumentException>(() => aportes.Criar(100m, origemSalarioId: 999));

            var aporte = aportes.Criar(250m, usuarioId);

            Assert.True(aporte.Id > 0);
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var aporte = contexto.AportesSimulados.Single();

            Assert.Equal(250m, aporte.ValorAportado);
            Assert.Equal(usuarioId, aporte.OrigemSalarioId);
            Assert.Equal(1, aporte.Id);
        }
    }
}
