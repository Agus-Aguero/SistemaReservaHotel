using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class AgregadoSoporteDolares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CotizacionAplicada",
                table: "Cobro",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonedaPago",
                table: "Cobro",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoEnDolares",
                table: "Cobro",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CotizacionAplicada",
                table: "Cobro");

            migrationBuilder.DropColumn(
                name: "MonedaPago",
                table: "Cobro");

            migrationBuilder.DropColumn(
                name: "MontoEnDolares",
                table: "Cobro");
        }
    }
}
