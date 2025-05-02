using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class ComprobanteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComprobanteController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IConverter _converter;



        public ComprobanteController(ApplicationDbContext context, ILogger<ComprobanteController> logger, UserManager<IdentityUser> userManager, IConverter converter)
        {
            _converter = converter;
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }



        public async Task<IActionResult> Index()
        {
            var identityUserId = _userManager.GetUserId(User);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);

            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var carrito = await _context.Carritos
                .Include(c => c.CarritoAutopartes)
                    .ThenInclude(cp => cp.Autoparte)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

            if (carrito == null || !carrito.CarritoAutopartes.Any())
            {
                return RedirectToAction("Index", "Carrito", new { mensaje = "Tu carrito está vacío" });
            }

            // Convertir los elementos del carrito a CarritoItem
            var carritoItems = carrito.CarritoAutopartes.Select(item => new Carrito.CarritoItem
            {
                Nombre = item.Autoparte.aut_nombre,
                PrecioUnitario = item.Autoparte.aut_precio,
                Cantidad = item.car_cantidad,
                ImagenUrl = item.Autoparte.aut_imagen
            }).ToList();

            // Crear el tuple 
            var modelo = new Tuple<Usuario, List<Carrito.CarritoItem>>(usuario, carritoItems);

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarComprobante(string tipoComprobante, string num_identificacion, string fac_razon, string fac_ruc, string fac_direccion)
        {
            Console.WriteLine($"Tipo de comprobante recibido: {tipoComprobante}");

            // 1. Obtener el usuario autenticado
            var identityUserId = _userManager.GetUserId(User);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);

            if (usuario == null)
            {
                return Unauthorized("Usuario no encontrado.");
            }

            // 2. Obtener el carrito del usuario
            var carrito = await _context.Carritos
    .Include(c => c.CarritoAutopartes)
        .ThenInclude(cp => cp.Autoparte)
    .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

            if (carrito == null || carrito.CarritoAutopartes == null || !carrito.CarritoAutopartes.Any())
            {
                return BadRequest("El carrito está vacío.");
            }

            // 3. Crear la venta
            var venta = new Venta
            {
                ven_fecha = DateOnly.FromDateTime(DateTime.Now),
                UsuarioId = usuario.usu_id,
                MetodoPagoId = 1, // Puedes modificar esto si es dinámico
                Total = carrito.car_total
            };

            _context.Venta.Add(venta);
            await _context.SaveChangesAsync(); // Para obtener el ID de venta

            // 4. Crear detalles de venta
            foreach (var item in carrito.CarritoAutopartes)
            {
                var detalle = new DetalleVentas
                {
                    VentaId = venta.ven_id,
                    AutoParteId = item.AutoparteId,
                    det_cantidad = item.car_cantidad
                };
                _context.DetalleVentas.Add(detalle);

                // 🔽 Reducir stock de autoparte
                var autoparte = await _context.Autopartes.FirstOrDefaultAsync(a => a.aut_id == item.AutoparteId);
                if (autoparte != null)
                {
                    if (autoparte.aut_cantidad < item.car_cantidad)
                    {
                        return BadRequest($"Stock insuficiente para la autoparte {autoparte.aut_nombre}.");
                    }

                    autoparte.aut_cantidad -= item.car_cantidad;
                    _context.Autopartes.Update(autoparte);
                }
            }
            await _context.SaveChangesAsync(); // Guardar los detalles de venta

            // 5. Crear el comprobante
            var comprobante = new Comprobante
            {
                tipo_comprobante = tipoComprobante,
                VentaId = venta.ven_id
            };
            _context.Comprobante.Add(comprobante);
            await _context.SaveChangesAsync();

            // 🔽 5.1 Crear las salidas a partir de los detalles de venta
            foreach (var item in carrito.CarritoAutopartes)
            {
                var salida = new Salida
                {
                    sal_fechasalida = DateOnly.FromDateTime(DateTime.Now),
                    sal_cantidad = item.car_cantidad,
                    ComprobanteId = comprobante.com_id,
                    AutoparteId = item.AutoparteId
                };
                _context.Salida.Add(salida);
            }
            await _context.SaveChangesAsync();

            // 6. Crear Boleta o Factura
            if (tipoComprobante.ToLower() == "boleta")
            {
                var boleta = new Boleta
                {
                    ComprobanteId = comprobante.com_id,
                    num_identificacion = num_identificacion,
                };
                _context.Boleta.Add(boleta);
            }
            else if (tipoComprobante.ToLower() == "factura")
            {
                var factura = new Factura
                {
                    ComprobanteId = comprobante.com_id,
                    fac_ruc = fac_ruc,
                    fac_razonsocial = fac_razon,
                    fac_direccion = fac_direccion
                };
                _context.Factura.Add(factura);
            }
            else
            {
                return BadRequest("Tipo de comprobante no válido. Debe ser 'boleta' o 'factura'.");
            }

            await _context.SaveChangesAsync();

            //7. Limpiar el carrito si lo deseas
            _context.CarritoAutopartes.RemoveRange(carrito.CarritoAutopartes);
            await _context.SaveChangesAsync();

            // Generar el PDF
            var htmlContent = GenerateComprobanteHtml(comprobante); // Función que creas para generar el HTML de comprobante
            var pdfBytes = ConvertHtmlToPdf(htmlContent);

            // Retornar el PDF
            return File(pdfBytes, "application/pdf", "comprobante.pdf");
        }

        private string GenerateComprobanteHtml(Comprobante comprobante)
        {
            // Cargar comprobante con relaciones necesarias
            var comprobanteCompleto = _context.Comprobante
                .Include(c => c.Venta)
                .Include(c => c.Boletas)
                .Include(c => c.Facturas)
                .FirstOrDefault(c => c.com_id == comprobante.com_id);


            if (comprobanteCompleto == null || comprobanteCompleto.Venta == null)
                return "<p>Error: Comprobante no válido.</p>";

            var venta = comprobanteCompleto.Venta;

            // Obtener detalles de venta
            var detallesVenta = _context.DetalleVentas
                .Where(dv => dv.VentaId == venta.ven_id)
                .Include(dv => dv.Autoparte)
                .ToList();

            // Iniciar HTML
            var htmlContent = $@"
                <html>
                <head>
                    <title>Comprobante de Pago</title>
                    <link rel='stylesheet' href='/css/comprobante.css' />
                </head>
                <header class='comprobante-info'>
                    <img src='/images/logo_marimon.png' alt='Logo de la Empresa' />
                    <h1>Comprobante Electrónico - Marimon</h1>
                </header>
                <body>
                    <p><strong>Tipo de comprobante:</strong> {comprobanteCompleto.tipo_comprobante}</p>
                    <p><strong>Fecha:</strong> {venta.ven_fecha}</p>
                    <p><strong>Venta ID:</strong> {venta.ven_id}</p>";

                        // Datos específicos según tipo
                        if (comprobanteCompleto.tipo_comprobante.ToLower() == "boleta" && comprobanteCompleto.Boletas.Any())
                        {
                            var boleta = comprobanteCompleto.Boletas.First();
                            htmlContent += $@"
                    <p><strong>ID Boleta:</strong> {boleta.bol_id}</p>
                    <p><strong>Número de Identificación:</strong> {boleta.num_identificacion}</p>";
                        }
                        else if (comprobanteCompleto.tipo_comprobante.ToLower() == "factura" && comprobanteCompleto.Facturas.Any())
                        {
                            var factura = comprobanteCompleto.Facturas.First();
                            htmlContent += $@"
                    <p><strong>Razón Social:</strong> {factura.fac_razonsocial}</p>
                    <p><strong>RUC:</strong> {factura.fac_ruc}</p>
                    <p><strong>Dirección:</strong> {factura.fac_direccion}</p>";
                        }

                        // Tabla de productos
                        htmlContent += $@"
                    <h3>Detalles de la compra:</h3>
                    <table border='1' cellpadding='5'>
                        <thead>
                            <tr>
                                <th>Autoparte</th>
                                <th>Cantidad</th>
                                <th>Precio Unitario</th>
                                <th>Subtotal</th>
                            </tr>
                        </thead>
                        <tbody>";

                        foreach (var detalle in detallesVenta)
                        {
                            var autoparte = detalle.Autoparte;
                            var subtotal = autoparte.aut_precio * detalle.det_cantidad;

                            htmlContent += $@"
                        <tr>
                            <td>{autoparte.aut_nombre}</td>
                            <td>{detalle.det_cantidad}</td>
                            <td>{autoparte.aut_precio:C}</td>
                            <td>{subtotal:C}</td>
                        </tr>";
                        }

                        htmlContent += $@"
                        </tbody>
                    </table>
                    <p><strong>Total:</strong> {venta.Total:C}</p>
                    <footer>
                    Este comprobante es generado electrónicamente y no requiere firma o sello.
                </footer>
                </body>
                </html>";
            return htmlContent;
        }



        private byte[] ConvertHtmlToPdf(string htmlContent)
        {

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
            ColorMode = ColorMode.Color,
            Orientation = Orientation.Portrait,
            PaperSize = PaperKind.A4
        },
                Objects = {
            new ObjectSettings() {
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" }
            }
        }
            };

            return _converter.Convert(doc);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}