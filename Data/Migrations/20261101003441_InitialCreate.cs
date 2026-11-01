using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Portfy.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ativos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, collation: "NOCASE"),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PrecoAtual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RentabilidadeAcumulada = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DividendYieldAnual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TaxaStakingAnual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    IndiceReferencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TaxaAdministracaoAnual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    RendimentoMensalPorCota = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TaxaAnualContratada = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ativos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carteiras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SaldoDisponivel = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carteiras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeCategoria = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LimiteDefinido = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ValorGastoAtual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PercentualDoSalario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SalarioMensal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DataRecebimento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Login = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    SenhaHash = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    ContaVerificada = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Posicoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AtivoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoMedio = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CarteiraSimuladaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posicoes_Ativos_AtivoId",
                        column: x => x.AtivoId,
                        principalTable: "Ativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Posicoes_Carteiras_CarteiraSimuladaId",
                        column: x => x.CarteiraSimuladaId,
                        principalTable: "Carteiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Aportes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DataAporte = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValorAportado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OrigemSalarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    CarteiraSimuladaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aportes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aportes_Carteiras_CarteiraSimuladaId",
                        column: x => x.CarteiraSimuladaId,
                        principalTable: "Carteiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aportes_Usuarios_OrigemSalarioId",
                        column: x => x.OrigemSalarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Ativos",
                columns: new[] { "Id", "DividendYieldAnual", "Nome", "PrecoAtual", "RentabilidadeAcumulada", "Ticker", "Tipo" },
                values: new object[,]
                {
                    { 1, 12.00m, "Petrobras PN", 38.50m, 0m, "PETR4", "Acao" },
                    { 2, 8.50m, "Vale ON", 62.10m, 0m, "VALE3", "Acao" }
                });

            migrationBuilder.InsertData(
                table: "Ativos",
                columns: new[] { "Id", "Nome", "PrecoAtual", "RendimentoMensalPorCota", "RentabilidadeAcumulada", "Ticker", "Tipo" },
                values: new object[] { 3, "CGHG Logística", 160.00m, 1.10m, 0m, "HGLG11", "Fii" });

            migrationBuilder.InsertData(
                table: "Ativos",
                columns: new[] { "Id", "Nome", "PrecoAtual", "RentabilidadeAcumulada", "TaxaStakingAnual", "Ticker", "Tipo" },
                values: new object[] { 4, "Bitcoin", 350000.00m, 0m, 0m, "BTC", "Cripto" });

            migrationBuilder.InsertData(
                table: "Carteiras",
                columns: new[] { "Id", "SaldoDisponivel" },
                values: new object[] { 1, 10000m });

            migrationBuilder.InsertData(
                table: "Orcamentos",
                columns: new[] { "Id", "LimiteDefinido", "NomeCategoria", "PercentualDoSalario", "ValorGastoAtual" },
                values: new object[] { 1, 1200m, "Alimentação", 24m, 0m });

            migrationBuilder.CreateIndex(
                name: "IX_Aportes_CarteiraSimuladaId",
                table: "Aportes",
                column: "CarteiraSimuladaId");

            migrationBuilder.CreateIndex(
                name: "IX_Aportes_OrigemSalarioId",
                table: "Aportes",
                column: "OrigemSalarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_Ticker",
                table: "Ativos",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posicoes_AtivoId",
                table: "Posicoes",
                column: "AtivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Posicoes_CarteiraSimuladaId",
                table: "Posicoes",
                column: "CarteiraSimuladaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Login",
                table: "Usuarios",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aportes");

            migrationBuilder.DropTable(
                name: "Orcamentos");

            migrationBuilder.DropTable(
                name: "Posicoes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Ativos");

            migrationBuilder.DropTable(
                name: "Carteiras");
        }
    }
}
