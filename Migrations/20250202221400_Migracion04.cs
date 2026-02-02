using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class Migracion04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Dni",
                table: "Persona",
                newName: "Genero");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Genero",
                table: "Persona",
                newName: "Dni");
        }
    }
}
