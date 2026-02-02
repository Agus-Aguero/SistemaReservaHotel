using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaReserva.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicialComposite01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Persona");

            migrationBuilder.AddColumn<int>(
                name: "PerfilId",
                table: "Usuario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Persona",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Componente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoComponente = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Componente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermisoRelacion",
                columns: table => new
                {
                    HijoId = table.Column<int>(type: "int", nullable: false),
                    PadreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisoRelacion", x => new { x.HijoId, x.PadreId });
                    table.ForeignKey(
                        name: "FK_PermisoRelacion_Componente_HijoId",
                        column: x => x.HijoId,
                        principalTable: "Componente",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermisoRelacion_Componente_PadreId",
                        column: x => x.PadreId,
                        principalTable: "Componente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PerfilId",
                table: "Usuario",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_PermisoRelacion_PadreId",
                table: "PermisoRelacion",
                column: "PadreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuario_Componente_PerfilId",
                table: "Usuario",
                column: "PerfilId",
                principalTable: "Componente",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuario_Componente_PerfilId",
                table: "Usuario");

            migrationBuilder.DropTable(
                name: "PermisoRelacion");

            migrationBuilder.DropTable(
                name: "Componente");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_PerfilId",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "PerfilId",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Persona");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Usuario",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "Persona",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
