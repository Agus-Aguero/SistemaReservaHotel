using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class QuitarDirecciondotnet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Persona");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Persona",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
