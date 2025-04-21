using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marimon.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReajusteSinServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "cat_nombre",
                table: "Categoria",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "aut_nombre",
                table: "Autoparte",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "aut_especificacion",
                table: "Autoparte",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "aut_descripcion",
                table: "Autoparte",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "aut_cantidad",
                table: "Autoparte",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Entradas",
                columns: table => new
                {
                    ent_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ent_cantidad = table.Column<int>(type: "integer", nullable: false),
                    ent_proveedor = table.Column<string>(type: "text", nullable: true),
                    ent_fechaent = table.Column<DateOnly>(type: "date", nullable: false),
                    AutoparteId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entradas", x => x.ent_id);
                    table.ForeignKey(
                        name: "FK_Entradas_Autoparte_AutoparteId",
                        column: x => x.AutoparteId,
                        principalTable: "Autoparte",
                        principalColumn: "aut_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetodoPago",
                columns: table => new
                {
                    pag_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pag_importe = table.Column<string>(type: "text", nullable: true),
                    pag_metodo = table.Column<string>(type: "text", nullable: true),
                    pag_fecha = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodoPago", x => x.pag_id);
                });

            migrationBuilder.CreateTable(
                name: "Servicio",
                columns: table => new
                {
                    ser_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ser_nombre = table.Column<string>(type: "text", nullable: false),
                    ser_descripcion = table.Column<string>(type: "text", nullable: false),
                    ser_imagen = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicio", x => x.ser_id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    usu_id = table.Column<string>(type: "text", nullable: false),
                    usu_nombre = table.Column<string>(type: "text", nullable: true),
                    usu_apellido = table.Column<string>(type: "text", nullable: true),
                    usu_nombrePerfil = table.Column<string>(type: "text", nullable: true),
                    usu_numIdentificacion = table.Column<string>(type: "text", nullable: true),
                    usu_correo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.usu_id);
                    table.ForeignKey(
                        name: "FK_Usuario_AspNetUsers_usu_id",
                        column: x => x.usu_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Venta",
                columns: table => new
                {
                    ven_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ven_fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venta", x => x.ven_id);
                    table.ForeignKey(
                        name: "FK_Venta_MetodoPago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "MetodoPago",
                        principalColumn: "pag_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Boleta",
                columns: table => new
                {
                    bol_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boleta", x => x.bol_id);
                    table.ForeignKey(
                        name: "FK_Boleta_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "usu_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Factura",
                columns: table => new
                {
                    fac_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac_razonsocial = table.Column<string>(type: "text", nullable: true),
                    fac_ruc = table.Column<string>(type: "text", nullable: true),
                    fac_direccion = table.Column<string>(type: "text", nullable: true),
                    UsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factura", x => x.fac_id);
                    table.ForeignKey(
                        name: "FK_Factura_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "usu_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reserva",
                columns: table => new
                {
                    res_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    res_placa = table.Column<string>(type: "text", nullable: false),
                    res_telefono = table.Column<string>(type: "text", nullable: false),
                    res_fecha = table.Column<string>(type: "text", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    ser_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reserva", x => x.res_id);
                    table.ForeignKey(
                        name: "FK_Reserva_Servicio_ser_id",
                        column: x => x.ser_id,
                        principalTable: "Servicio",
                        principalColumn: "ser_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reserva_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "usu_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetalleVentas",
                columns: table => new
                {
                    det_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    det_cantidad = table.Column<string>(type: "text", nullable: true),
                    VentaId = table.Column<int>(type: "integer", nullable: false),
                    AutoParteId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleVentas", x => x.det_id);
                    table.ForeignKey(
                        name: "FK_DetalleVentas_Autoparte_AutoParteId",
                        column: x => x.AutoParteId,
                        principalTable: "Autoparte",
                        principalColumn: "aut_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetalleVentas_Venta_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Venta",
                        principalColumn: "ven_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comprobante",
                columns: table => new
                {
                    com_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    com_nombre = table.Column<string>(type: "text", nullable: true),
                    FacturaId = table.Column<int>(type: "integer", nullable: true),
                    BoletaId = table.Column<int>(type: "integer", nullable: true),
                    VentaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobante", x => x.com_id);
                    table.ForeignKey(
                        name: "FK_Comprobante_Boleta_BoletaId",
                        column: x => x.BoletaId,
                        principalTable: "Boleta",
                        principalColumn: "bol_id");
                    table.ForeignKey(
                        name: "FK_Comprobante_Factura_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Factura",
                        principalColumn: "fac_id");
                    table.ForeignKey(
                        name: "FK_Comprobante_Venta_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Venta",
                        principalColumn: "ven_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Salida",
                columns: table => new
                {
                    sal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sal_fechasalida = table.Column<DateOnly>(type: "date", nullable: false),
                    sal_cantidad = table.Column<int>(type: "integer", nullable: false),
                    ComprobanteId = table.Column<int>(type: "integer", nullable: true),
                    AutoparteId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salida", x => x.sal_id);
                    table.ForeignKey(
                        name: "FK_Salida_Autoparte_AutoparteId",
                        column: x => x.AutoparteId,
                        principalTable: "Autoparte",
                        principalColumn: "aut_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Salida_Comprobante_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobante",
                        principalColumn: "com_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boleta_UsuarioId",
                table: "Boleta",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_BoletaId",
                table: "Comprobante",
                column: "BoletaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_FacturaId",
                table: "Comprobante",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobante_VentaId",
                table: "Comprobante",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_AutoParteId",
                table: "DetalleVentas",
                column: "AutoParteId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_VentaId",
                table: "DetalleVentas",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_AutoparteId",
                table: "Entradas",
                column: "AutoparteId");

            migrationBuilder.CreateIndex(
                name: "IX_Factura_UsuarioId",
                table: "Factura",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_ser_id",
                table: "Reserva",
                column: "ser_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_UsuarioId",
                table: "Reserva",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Salida_AutoparteId",
                table: "Salida",
                column: "AutoparteId");

            migrationBuilder.CreateIndex(
                name: "IX_Salida_ComprobanteId",
                table: "Salida",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_MetodoPagoId",
                table: "Venta",
                column: "MetodoPagoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleVentas");

            migrationBuilder.DropTable(
                name: "Entradas");

            migrationBuilder.DropTable(
                name: "Reserva");

            migrationBuilder.DropTable(
                name: "Salida");

            migrationBuilder.DropTable(
                name: "Servicio");

            migrationBuilder.DropTable(
                name: "Comprobante");

            migrationBuilder.DropTable(
                name: "Boleta");

            migrationBuilder.DropTable(
                name: "Factura");

            migrationBuilder.DropTable(
                name: "Venta");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "MetodoPago");

            migrationBuilder.DropColumn(
                name: "aut_cantidad",
                table: "Autoparte");

            migrationBuilder.AlterColumn<string>(
                name: "cat_nombre",
                table: "Categoria",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "aut_nombre",
                table: "Autoparte",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "aut_especificacion",
                table: "Autoparte",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "aut_descripcion",
                table: "Autoparte",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
