using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class ActualizacionSeguridadGrupos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuario_Componente_PerfilId",
                table: "Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_PerfilId",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "PerfilId",
                table: "Usuario");

            migrationBuilder.CreateTable(
                name: "FamiliaUsuario",
                columns: table => new
                {
                    GruposIdComponente = table.Column<int>(type: "int", nullable: false),
                    UsuariosIdUsuario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamiliaUsuario", x => new { x.GruposIdComponente, x.UsuariosIdUsuario });
                    table.ForeignKey(
                        name: "FK_FamiliaUsuario_Componente_GruposIdComponente",
                        column: x => x.GruposIdComponente,
                        principalTable: "Componente",
                        principalColumn: "IdComponente",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamiliaUsuario_Usuario_UsuariosIdUsuario",
                        column: x => x.UsuariosIdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamiliaUsuario_UsuariosIdUsuario",
                table: "FamiliaUsuario",
                column: "UsuariosIdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamiliaUsuario");

            migrationBuilder.AddColumn<int>(
                name: "PerfilId",
                table: "Usuario",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PerfilId",
                table: "Usuario",
                column: "PerfilId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuario_Componente_PerfilId",
                table: "Usuario",
                column: "PerfilId",
                principalTable: "Componente",
                principalColumn: "IdComponente");
        }
    }
}
