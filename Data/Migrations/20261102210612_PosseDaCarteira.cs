using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfy.Data.Migrations
{
    /// <summary>
    /// Posse por usuário (arch §9.1): Carteiras e Orcamentos ganham dono,
    /// Aportes e Posicoes passam a ter a carteira explícita e não anulável.
    ///
    /// A migration é escrita à mão no Up porque o banco antigo pode conter dados:
    /// primeiro adiciona colunas anuláveis, depois associa o que existe ao primeiro
    /// usuário, cria os dados iniciais para os demais usuários e só então torna as
    /// colunas obrigatórias. Banco novo (sem usuários) perde apenas as linhas de
    /// seed antigas, que não pertenciam a ninguém.
    /// </summary>
    public partial class PosseDaCarteira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Colunas novas, anuláveis durante o backfill.
            migrationBuilder.DropIndex(
                name: "IX_Posicoes_CarteiraSimuladaId",
                table: "Posicoes");

            migrationBuilder.AddColumn<bool>(
                name: "EhFixo",
                table: "Orcamentos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Orcamentos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Carteiras",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Carteiras",
                type: "INTEGER",
                nullable: true);

            // 2. Backfill das linhas que já existiam.

            // Nome das carteiras antigas: a primeira é a Principal; as demais
            // recebem um nome derivado do Id para não colidir no índice único.
            migrationBuilder.Sql(
                "UPDATE Carteiras " +
                "SET Nome = CASE WHEN Id = 1 THEN 'Principal' ELSE 'Carteira ' || Id END " +
                "WHERE Nome IS NULL OR Nome = '';");

            // A carteira e o orçamento antigos passam para o primeiro usuário.
            migrationBuilder.Sql(
                "UPDATE Carteiras " +
                "SET UsuarioId = (SELECT MIN(Id) FROM Usuarios) " +
                "WHERE UsuarioId IS NULL AND EXISTS (SELECT 1 FROM Usuarios);");

            migrationBuilder.Sql(
                "UPDATE Orcamentos " +
                "SET UsuarioId = (SELECT MIN(Id) FROM Usuarios) " +
                "WHERE UsuarioId IS NULL AND EXISTS (SELECT 1 FROM Usuarios);");

            // Cada usuário sem carteira recebe a simulação Principal com o saldo
            // inicial que antes era global. Decimal no SQLite é TEXT (ver AppDbContext).
            migrationBuilder.Sql(
                "INSERT INTO Carteiras (UsuarioId, Nome, SaldoDisponivel) " +
                "SELECT u.Id, 'Principal', '10000.0' " +
                "FROM Usuarios u " +
                "WHERE NOT EXISTS (SELECT 1 FROM Carteiras c WHERE c.UsuarioId = u.Id);");

            // Cada usuário sem orçamento recebe a categoria padrão.
            migrationBuilder.Sql(
                "INSERT INTO Orcamentos " +
                "(UsuarioId, NomeCategoria, LimiteDefinido, PercentualDoSalario, ValorGastoAtual, EhFixo) " +
                "SELECT u.Id, 'Alimentação', '1200.0', '24.0', '0.0', 0 " +
                "FROM Usuarios u " +
                "WHERE NOT EXISTS (SELECT 1 FROM Orcamentos o WHERE o.UsuarioId = u.Id);");

            // Aporte antigo aponta para a carteira do usuário de origem.
            migrationBuilder.Sql(
                "UPDATE Aportes " +
                "SET CarteiraSimuladaId = (" +
                "    SELECT c.Id FROM Carteiras c " +
                "    WHERE c.UsuarioId = Aportes.OrigemSalarioId " +
                "    ORDER BY c.Id LIMIT 1) " +
                "WHERE CarteiraSimuladaId IS NULL;");

            // Se a origem não existe mais, usa a primeira carteira disponível.
            migrationBuilder.Sql(
                "UPDATE Aportes " +
                "SET CarteiraSimuladaId = (SELECT MIN(Id) FROM Carteiras) " +
                "WHERE CarteiraSimuladaId IS NULL AND EXISTS (SELECT 1 FROM Carteiras);");

            // Posição antiga aponta para a primeira carteira disponível.
            migrationBuilder.Sql(
                "UPDATE Posicoes " +
                "SET CarteiraSimuladaId = (SELECT MIN(Id) FROM Carteiras) " +
                "WHERE CarteiraSimuladaId IS NULL AND EXISTS (SELECT 1 FROM Carteiras);");

            // 3. Órfãos não podem virar NOT NULL. Sem usuários, as linhas de seed
            // antigas não pertencem a ninguém e são removidas (o cadastro passa a
            // criar carteira e orçamento por usuário).
            // Remove primeiro o que referencia as carteiras órfãs: Aportes tem
            // FK Restrict e impediria o DELETE da carteira.
            migrationBuilder.Sql(
                "DELETE FROM Aportes " +
                "WHERE CarteiraSimuladaId IS NULL " +
                "   OR CarteiraSimuladaId IN (SELECT Id FROM Carteiras WHERE UsuarioId IS NULL);");

            migrationBuilder.Sql(
                "DELETE FROM Posicoes " +
                "WHERE CarteiraSimuladaId IS NULL " +
                "   OR CarteiraSimuladaId IN (SELECT Id FROM Carteiras WHERE UsuarioId IS NULL);");

            migrationBuilder.Sql("DELETE FROM Carteiras WHERE UsuarioId IS NULL;");
            migrationBuilder.Sql("DELETE FROM Orcamentos WHERE UsuarioId IS NULL;");

            // 4. Colunas obrigatórias.
            migrationBuilder.AlterColumn<int>(
                name: "CarteiraSimuladaId",
                table: "Posicoes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CarteiraSimuladaId",
                table: "Aportes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Carteiras",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Carteiras",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Orcamentos",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            // 5. Índices e chaves estrangeiras do modelo por usuário.
            migrationBuilder.CreateIndex(
                name: "IX_Posicoes_CarteiraSimuladaId_AtivoId",
                table: "Posicoes",
                columns: new[] { "CarteiraSimuladaId", "AtivoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_UsuarioId_NomeCategoria",
                table: "Orcamentos",
                columns: new[] { "UsuarioId", "NomeCategoria" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Carteiras_UsuarioId_Nome",
                table: "Carteiras",
                columns: new[] { "UsuarioId", "Nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Carteiras_Usuarios_UsuarioId",
                table: "Carteiras",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orcamentos_Usuarios_UsuarioId",
                table: "Orcamentos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Voltar ao esquema global perde a noção de dono; para não falhar por
            // chave estrangeira, os dados financeiros são removidos antes do rollback.
            migrationBuilder.Sql("DELETE FROM Aportes;");
            migrationBuilder.Sql("DELETE FROM Posicoes;");
            migrationBuilder.Sql("DELETE FROM Carteiras;");
            migrationBuilder.Sql("DELETE FROM Orcamentos;");

            migrationBuilder.DropForeignKey(
                name: "FK_Carteiras_Usuarios_UsuarioId",
                table: "Carteiras");

            migrationBuilder.DropForeignKey(
                name: "FK_Orcamentos_Usuarios_UsuarioId",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Posicoes_CarteiraSimuladaId_AtivoId",
                table: "Posicoes");

            migrationBuilder.DropIndex(
                name: "IX_Orcamentos_UsuarioId_NomeCategoria",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Carteiras_UsuarioId_Nome",
                table: "Carteiras");

            migrationBuilder.DropColumn(
                name: "EhFixo",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Carteiras");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Carteiras");

            migrationBuilder.AlterColumn<int>(
                name: "CarteiraSimuladaId",
                table: "Posicoes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "CarteiraSimuladaId",
                table: "Aportes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.InsertData(
                table: "Carteiras",
                columns: new[] { "Id", "SaldoDisponivel" },
                values: new object[] { 1, 10000m });

            migrationBuilder.InsertData(
                table: "Orcamentos",
                columns: new[] { "Id", "LimiteDefinido", "NomeCategoria", "PercentualDoSalario", "ValorGastoAtual" },
                values: new object[] { 1, 1200m, "Alimentação", 24m, 0m });

            migrationBuilder.CreateIndex(
                name: "IX_Posicoes_CarteiraSimuladaId",
                table: "Posicoes",
                column: "CarteiraSimuladaId");
        }
    }
}
