using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class ModificacionReservaTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reserva_Habitacion_IdHabitacion",
                table: "Reserva");

            migrationBuilder.RenameColumn(
                name: "FechaSalida",
                table: "Reserva",
                newName: "FechaInicio");

            migrationBuilder.RenameColumn(
                name: "FechaEntrada",
                table: "Reserva",
                newName: "FechaFin");

            migrationBuilder.AlterColumn<int>(
                name: "IdHabitacion",
                table: "Reserva",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "IdTipoHabitacion",
                table: "Reserva",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_IdTipoHabitacion",
                table: "Reserva",
                column: "IdTipoHabitacion");

            migrationBuilder.AddForeignKey(
                name: "FK_Reserva_Habitacion_IdHabitacion",
                table: "Reserva",
                column: "IdHabitacion",
                principalTable: "Habitacion",
                principalColumn: "IdHabitacion");

            migrationBuilder.AddForeignKey(
                name: "FK_Reserva_TipoHabitacion_IdTipoHabitacion",
                table: "Reserva",
                column: "IdTipoHabitacion",
                principalTable: "TipoHabitacion",
                principalColumn: "IdTipoHabitacion",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reserva_Habitacion_IdHabitacion",
                table: "Reserva");

            migrationBuilder.DropForeignKey(
                name: "FK_Reserva_TipoHabitacion_IdTipoHabitacion",
                table: "Reserva");

            migrationBuilder.DropIndex(
                name: "IX_Reserva_IdTipoHabitacion",
                table: "Reserva");

            migrationBuilder.DropColumn(
                name: "IdTipoHabitacion",
                table: "Reserva");

            migrationBuilder.RenameColumn(
                name: "FechaInicio",
                table: "Reserva",
                newName: "FechaSalida");

            migrationBuilder.RenameColumn(
                name: "FechaFin",
                table: "Reserva",
                newName: "FechaEntrada");

            migrationBuilder.AlterColumn<int>(
                name: "IdHabitacion",
                table: "Reserva",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reserva_Habitacion_IdHabitacion",
                table: "Reserva",
                column: "IdHabitacion",
                principalTable: "Habitacion",
                principalColumn: "IdHabitacion",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
