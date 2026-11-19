using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfy.Data;
using Portfy.Models;
using Portfy.Services;

namespace Portfy.Tests.Data;

/// Prova de que o estado nomeado da carteira é persistido e de que uma transição
/// proibida não deixa o registro em situação inconsistente. Cada teste lê o banco
/// com um AppDbContext NOVO, como se a API tivesse sido reiniciada.
public class StatusDaCarteiraPersistenciaTests : IDisposable
{
    private readonly SqliteConnection _conexao;

    public StatusDaCarteiraPersistenciaTests()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();

        using AppDbContext contexto = NovoContexto();
        contexto.Database.Migrate();
    }

    public void Dispose() => _conexao.Dispose();

    private AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options);

    private static int CriarUsuarioComCarteira(AppDbContext contexto, string login) =>
        new UsuarioService(contexto, new SenhaHasher())
            .Cadastrar("Ana", 3000m, login, "segredo123")
            .Id;

    private static CarteiraSimulada CarteiraDo(AppDbContext contexto, int usuarioId) =>
        contexto.CarteirasSimuladas.First(c => c.UsuarioId == usuarioId);

    [Fact]
    public void CarteiraCriadaPeloCadastro_NasceAtivaNoBanco()
    {
        int usuarioId;
        using (AppDbContext contexto = NovoContexto())
            usuarioId = CriarUsuarioComCarteira(contexto, "estado1@portfy.test");

        using AppDbContext leitura = NovoContexto();

        Assert.Equal(StatusCarteira.Ativa, CarteiraDo(leitura, usuarioId).Status);
    }

    [Fact]
    public void Suspender_GravaEstadoESobreviveATrocaDeContexto()
    {
        int usuarioId;
        using (AppDbContext contexto = NovoContexto())
        {
            usuarioId = CriarUsuarioComCarteira(contexto, "estado2@portfy.test");

            CarteiraSimulada carteira = CarteiraDo(contexto, usuarioId);
            carteira.Suspender();
            contexto.SaveChanges();
        }

        using AppDbContext leitura = NovoContexto();

        Assert.Equal(StatusCarteira.Suspensa, CarteiraDo(leitura, usuarioId).Status);
    }

    [Fact]
    public void TransicaoProibida_NaoAlteraRegistroPersistido()
    {
        int usuarioId;
        decimal saldoAntes;
        using (AppDbContext contexto = NovoContexto())
        {
            usuarioId = CriarUsuarioComCarteira(contexto, "estado3@portfy.test");

            CarteiraSimulada carteira = CarteiraDo(contexto, usuarioId);
            carteira.Encerrar();
            contexto.SaveChanges();

            saldoAntes = carteira.SaldoDisponivel;
        }

        using (AppDbContext contexto = NovoContexto())
        {
            CarteiraSimulada carteira = CarteiraDo(contexto, usuarioId);

            // Reativar uma carteira encerrada é transição proibida: o domínio recusa
            // antes de qualquer mutação, então não há o que salvar nem dado a corrigir.
            Assert.Throws<InvalidOperationException>(() => carteira.Reativar());
            Assert.Equal(StatusCarteira.Encerrada, carteira.Status);

            contexto.SaveChanges();
        }

        using AppDbContext contextoFinal = NovoContexto();
        CarteiraSimulada relida = CarteiraDo(contextoFinal, usuarioId);

        Assert.Equal(StatusCarteira.Encerrada, relida.Status);
        Assert.Equal(saldoAntes, relida.SaldoDisponivel);
    }

    [Fact]
    public void CarteiraSuspensa_Persistida_RecusaCompraSemGravarSaldo()
    {
        int usuarioId;
        decimal saldoAntes;
        using (AppDbContext contexto = NovoContexto())
        {
            usuarioId = CriarUsuarioComCarteira(contexto, "estado4@portfy.test");

            CarteiraSimulada carteira = CarteiraDo(contexto, usuarioId);
            carteira.Suspender();
            contexto.SaveChanges();

            saldoAntes = carteira.SaldoDisponivel;
        }

        using (AppDbContext contexto = NovoContexto())
        {
            // O cadastro já semeia o catálogo; usar um ativo existente evita
            // colidir com o índice único de Ticker.
            Ativo ativo = contexto.Ativos.First();

            CarteiraSimulada carteira = CarteiraDo(contexto, usuarioId);

            Assert.Throws<InvalidOperationException>(() => carteira.ComprarAtivo(ativo, 1));
            contexto.SaveChanges();
        }

        using AppDbContext contextoFinal = NovoContexto();
        CarteiraSimulada relida = contextoFinal.CarteirasSimuladas
            .Include(c => c.Posicoes)
            .First(c => c.UsuarioId == usuarioId);

        Assert.Equal(StatusCarteira.Suspensa, relida.Status);
        Assert.Equal(saldoAntes, relida.SaldoDisponivel);
        Assert.Empty(relida.Posicoes);
    }
}
