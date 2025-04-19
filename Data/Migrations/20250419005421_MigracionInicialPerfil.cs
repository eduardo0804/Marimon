using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marimon.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicialPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "usu_bloqueohab",
                table: "t_usuario");

            migrationBuilder.DropColumn(
                name: "usu_cierrepat",
                table: "t_usuario");

            migrationBuilder.DropColumn(
                name: "usu_contrasenia",
                table: "t_usuario");

            migrationBuilder.DropColumn(
                name: "usu_correo_verificado",
                table: "t_usuario");

            migrationBuilder.DropColumn(
                name: "usu_recuento",
                table: "t_usuario");

            migrationBuilder.RenameColumn(
                name: "usu_selloseg",
                table: "t_usuario",
                newName: "usu_numIdentificacion");

            migrationBuilder.RenameColumn(
                name: "usu_dni",
                table: "t_usuario",
                newName: "usu_nombrePerfil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "usu_numIdentificacion",
                table: "t_usuario",
                newName: "usu_selloseg");

            migrationBuilder.RenameColumn(
                name: "usu_nombrePerfil",
                table: "t_usuario",
                newName: "usu_dni");

            migrationBuilder.AddColumn<bool>(
                name: "usu_bloqueohab",
                table: "t_usuario",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usu_cierrepat",
                table: "t_usuario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usu_contrasenia",
                table: "t_usuario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "usu_correo_verificado",
                table: "t_usuario",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "usu_recuento",
                table: "t_usuario",
                type: "integer",
                nullable: true);
        }
    }
}
