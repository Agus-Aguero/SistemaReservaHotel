using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class RelacionHabitacionTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioNoche",
                table: "Habitacion");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Habitacion");

            migrationBuilder.AddColumn<int>(
                name: "IdTipoHabitacion",
                table: "Habitacion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Habitacion_IdTipoHabitacion",
                table: "Habitacion",
                column: "IdTipoHabitacion");

            migrationBuilder.AddForeignKey(
                name: "FK_Habitacion_TipoHabitacion_IdTipoHabitacion",
                table: "Habitacion",
                column: "IdTipoHabitacion",
                principalTable: "TipoHabitacion",
                principalColumn: "IdTipoHabitacion",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Habitacion_TipoHabitacion_IdTipoHabitacion",
                table: "Habitacion");

            migrationBuilder.DropIndex(
                name: "IX_Habitacion_IdTipoHabitacion",
                table: "Habitacion");

            migrationBuilder.DropColumn(
                name: "IdTipoHabitacion",
                table: "Habitacion");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioNoche",
                table: "Habitacion",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Habitacion",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
