using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marimon.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicialUsuarioS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_t_usuario_AspNetUsers_usu_id",
                table: "t_usuario",
                column: "usu_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_usuario_AspNetUsers_usu_id",
                table: "t_usuario");
        }
    }
}
