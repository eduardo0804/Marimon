using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marimon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSalCantidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "aut_cantidad",
                table: "Salida",
                newName: "sal_cantidad");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sal_cantidad",
                table: "Salida",
                newName: "aut_cantidad");
        }
    }
}
