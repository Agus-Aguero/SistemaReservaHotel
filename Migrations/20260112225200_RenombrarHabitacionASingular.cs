using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarHabitacionASingular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Habitaciones",
                table: "Habitaciones");

            migrationBuilder.RenameTable(
                name: "Habitaciones",
                newName: "Habitacion");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Habitacion",
                table: "Habitacion",
                column: "IdHabitacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Habitacion",
                table: "Habitacion");

            migrationBuilder.RenameTable(
                name: "Habitacion",
                newName: "Habitaciones");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Habitaciones",
                table: "Habitaciones",
                column: "IdHabitacion");
        }
    }
}
