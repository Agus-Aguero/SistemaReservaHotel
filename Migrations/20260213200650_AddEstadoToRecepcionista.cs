using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class AddEstadoToRecepcionista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Persona",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Persona");
        }
    }
}
