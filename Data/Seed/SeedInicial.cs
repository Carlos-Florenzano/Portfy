using Microsoft.EntityFrameworkCore;
using Portfy.Models;

namespace Portfy.Data.Seed;

/// Dados iniciais aplicados por migration (ADR-016). É idempotente por construção:
/// as linhas entram uma única vez, na criação do banco, e nunca sobrescrevem
/// alterações legítimas feitas depois (preço simulado, saldo, gasto do orçamento).
/// Os valores abaixo são exatamente os que existiam em memória no catálogo do
/// AtivoService e nos serviços de carteira e orçamento — nada foi inventado.
///
/// Carteira e orçamento NÃO entram mais no seed: eles pertencem a um usuário
/// (arch §9.1) e são criados no cadastro, na mesma transação do usuário. As
/// constantes abaixo definem os valores iniciais usados nessa criação.
public static class SeedInicial
{
    // Saldo inicial que a CarteiraService criava em memória.
    public const decimal SaldoInicialCarteira = 10000m;

    // Nome da primeira simulação de cada usuário.
    public const string NomeCarteiraPadrao = "Principal";

    // Orçamento de exemplo que a OrcamentoService criava em memória.
    public const string CategoriaPadrao = "Alimentação";
    public const decimal LimitePadrao = 1200m;
    public const decimal PercentualPadrao = 24m;

    public static void Aplicar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Acao>().HasData(
            new
            {
                Id = 1,
                Ticker = "PETR4",
                Nome = "Petrobras PN",
                PrecoAtual = 38.50m,
                RentabilidadeAcumulada = 0m,
                DividendYieldAnual = 12.00m
            },
            new
            {
                Id = 2,
                Ticker = "VALE3",
                Nome = "Vale ON",
                PrecoAtual = 62.10m,
                RentabilidadeAcumulada = 0m,
                DividendYieldAnual = 8.50m
            });

        modelBuilder.Entity<FundoImobiliario>().HasData(
            new
            {
                Id = 3,
                Ticker = "HGLG11",
                Nome = "CGHG Logística",
                PrecoAtual = 160.00m,
                RentabilidadeAcumulada = 0m,
                RendimentoMensalPorCota = 1.10m
            });

        modelBuilder.Entity<Cripto>().HasData(
            new
            {
                Id = 4,
                Ticker = "BTC",
                Nome = "Bitcoin",
                PrecoAtual = 350000.00m,
                RentabilidadeAcumulada = 0m,
                TaxaStakingAnual = 0m
            });
    }
}
