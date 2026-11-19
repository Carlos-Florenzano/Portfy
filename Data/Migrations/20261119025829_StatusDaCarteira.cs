using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfy.Data.Migrations
{
    /// <inheritdoc />
    public partial class StatusDaCarteira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Carteiras",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ativa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Carteiras");
        }
    }
}
