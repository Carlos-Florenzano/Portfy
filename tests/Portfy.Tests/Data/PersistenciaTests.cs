using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Models;
using Portfy.Services;
using Portfy.Services.Contracts;

namespace Portfy.Tests.Data;

/// Prova de persistência real: cada teste usa um AppDbContext novo sobre o MESMO banco
/// SQLite em memória (equivalente a reiniciar a API). Se algum serviço voltasse a guardar
/// estado em coleção C#, estas asserções falhariam na segunda leitura.
///
/// Depois da migration PosseDaCarteira, carteira e orçamento pertencem ao usuário:
/// os testes criam a conta e leem os dados pelo id do dono, nunca por um Id global.
public class PersistenciaTests : IDisposable
{
    private readonly SqliteConnection _conexao;

    public PersistenciaTests()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();

        using AppDbContext contexto = NovoContexto();
        contexto.Database.Migrate(); // mesmo esquema do portfy.db, criado por migration
    }

    public void Dispose() => _conexao.Dispose();

    private AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);

    private static Usuario CriarUsuario(AppDbContext contexto, string login) =>
        new UsuarioService(contexto, new SenhaHasher())
            .Cadastrar("Ana", 3000m, login, "segredo123");

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
            id = CriarUsuario(contexto, "ana@portfy.test").Id;
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var usuario = new UsuarioService(contexto, new SenhaHasher())
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
            new UsuarioService(contexto, new SenhaHasher()).Cadastrar("Bruno", 1234.56m, "bruno@portfy.test", "segredo456");
            new UsuarioService(contexto, new SenhaHasher()).Cadastrar("Carla", 5678.987654m, "carla@portfy.test", "segredo789");
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
        int usuarioId;

        using (AppDbContext contexto = NovoContexto())
        {
            usuarioId = CriarUsuario(contexto, "ana@portfy.test").Id;

            var carteira = new CarteiraService(contexto).ObterCarteira(usuarioId);
            var petr4 = contexto.Ativos.Single(a => a.Ticker == "PETR4");

            carteira.ComprarAtivo(petr4, 10);
            contexto.SaveChanges();
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var carteira = new CarteiraService(contexto).ObterCarteira(usuarioId);
            var posicao = Assert.Single(carteira.Posicoes);

            Assert.Equal(10, posicao.Quantidade);
            Assert.Equal(38.50m, posicao.PrecoMedio);
            Assert.Equal("PETR4", posicao.Ativo.Ticker);
            Assert.Equal(9615.00m, carteira.SaldoDisponivel);
            Assert.Equal(usuarioId, carteira.UsuarioId);
        }
    }

    [Fact]
    public void CarteirasDeUsuariosDiferentes_NaoSeMisturam()
    {
        int anaId;
        int brunoId;

        using (AppDbContext contexto = NovoContexto())
        {
            anaId = CriarUsuario(contexto, "ana@portfy.test").Id;
            brunoId = CriarUsuario(contexto, "bruno@portfy.test").Id;

            var carteiraAna = new CarteiraService(contexto).ObterCarteira(anaId);
            var petr4 = contexto.Ativos.Single(a => a.Ticker == "PETR4");

            carteiraAna.ComprarAtivo(petr4, 10);
            contexto.SaveChanges();
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var service = new CarteiraService(contexto);

            var carteiraAna = service.ObterCarteira(anaId);
            var carteiraBruno = service.ObterCarteira(brunoId);

            Assert.Single(carteiraAna.Posicoes);
            Assert.Empty(carteiraBruno.Posicoes);
            Assert.Equal(9615m, carteiraAna.SaldoDisponivel);
            Assert.Equal(10000m, carteiraBruno.SaldoDisponivel);
        }
    }

    [Fact]
    public void CompraEVenda_AtualizamSaldoEPosicaoNoBanco()
    {
        int usuarioId;

        using (AppDbContext contexto = NovoContexto())
        {
            usuarioId = CriarUsuario(contexto, "ana@portfy.test").Id;

            var operacaoCompra = new CarteiraService(contexto).Comprar(usuarioId, "PETR4", 10);

            Assert.NotNull(operacaoCompra);
            Assert.Equal(385m, operacaoCompra!.ValorTotal);
            Assert.Equal(9615m, operacaoCompra.Carteira.SaldoDisponivel);
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var carteira = new CarteiraService(contexto).ObterCarteira(usuarioId);
            Assert.Single(carteira.Posicoes);
            Assert.Equal(9615m, carteira.SaldoDisponivel);
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var operacaoVenda = new CarteiraService(contexto).Vender(usuarioId, "PETR4", 10);

            Assert.NotNull(operacaoVenda);
            Assert.Equal(10000m, operacaoVenda!.Carteira.SaldoDisponivel);
            Assert.Empty(operacaoVenda.Carteira.Posicoes);
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var carteira = new CarteiraService(contexto).ObterCarteira(usuarioId);
            Assert.Empty(carteira.Posicoes);
            Assert.Equal(10000m, carteira.SaldoDisponivel);
        }
    }

    [Fact]
    public void Aporte_ExigeUsuarioExistenteEGeraIdNoBanco()
    {
        int usuarioId;

        using (AppDbContext contexto = NovoContexto())
        {
            var aportes = new AporteService(contexto, new CarteiraService(contexto));

            // Usuário inexistente: o serviço recusa antes de criar carteira órfã.
            Assert.Throws<KeyNotFoundException>(() => aportes.Criar(999, 100m));

            usuarioId = CriarUsuario(contexto, "ana@portfy.test").Id;

            AporteRealizado realizado = aportes.Criar(usuarioId, 250m);

            Assert.True(realizado.Aporte.Id > 0);
            Assert.Equal(usuarioId, realizado.Aporte.OrigemSalarioId);
            Assert.Equal(10250m, realizado.SaldoDisponivel);
            Assert.Equal(10250m, realizado.PatrimonioTotal);
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var aporte = contexto.AportesSimulados.Single();

            Assert.Equal(250m, aporte.ValorAportado);
            Assert.Equal(usuarioId, aporte.OrigemSalarioId);
            Assert.True(aporte.CarteiraId > 0);
            Assert.Equal(1, aporte.Id);

            var carteira = new CarteiraService(contexto).ObterCarteira(usuarioId);
            Assert.Equal(10250m, carteira.SaldoDisponivel);
            Assert.Single(carteira.Aportes);
        }
    }

    [Fact]
    public void Orcamento_IsoladoPorUsuario()
    {
        int anaId;
        int brunoId;

        using (AppDbContext contexto = NovoContexto())
        {
            anaId = CriarUsuario(contexto, "ana@portfy.test").Id;
            brunoId = CriarUsuario(contexto, "bruno@portfy.test").Id;

            new OrcamentoService(contexto).AdicionarDespesa(anaId, 200m);
        }

        using (AppDbContext contexto = NovoContexto())
        {
            var service = new OrcamentoService(contexto);

            Assert.Equal(200m, service.ObterOrcamento(anaId).ValorGastoAtual);
            Assert.Equal(0m, service.ObterOrcamento(brunoId).ValorGastoAtual);
            Assert.Equal("Alimentação", service.ObterOrcamento(brunoId).NomeCategoria);
        }
    }
}
