using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class ModificacionIDUsuario01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reserva_Usuario_IdUsuario",
                table: "Reserva");

            migrationBuilder.AddForeignKey(
                name: "FK_Reserva_Usuario_IdUsuario",
                table: "Reserva",
                column: "IdUsuario",
                principalTable: "Usuario",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reserva_Usuario_IdUsuario",
                table: "Reserva");

            migrationBuilder.AddForeignKey(
                name: "FK_Reserva_Usuario_IdUsuario",
                table: "Reserva",
                column: "IdUsuario",
                principalTable: "Usuario",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
