using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using Marimon.Data;
using Marimon.Models;
using Marimon.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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

         private readonly IEmailSenderWithAttachments _emailSender; // Cambio aquí



        public ComprobanteController(ApplicationDbContext context, ILogger<ComprobanteController> logger, UserManager<IdentityUser> userManager, IConverter converter, IEmailSenderWithAttachments emailSender)
        {
            _converter = converter;
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
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
            var htmlContent = GenerateComprobanteHtml(comprobante);
            var pdfBytes = ConvertHtmlToPdf(htmlContent);
            
            // Obtener el correo del usuario para enviar el comprobante
            var user = await _userManager.FindByIdAsync(usuario.usu_id);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    // Crear el correo con el PDF como adjunto
                    var nombreCompleto = usuario.usu_nombre + " " + usuario.usu_apellido;
                    var emailHtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                            <div style='text-align: center; margin-bottom: 20px;'>
                                <img src='https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png' alt='Marimon Logo' style='max-width: 200px;' />
                            </div>
                            <h2 style='color: #e62020;'>¡Gracias por tu compra!</h2>
                            <p>Hola <strong>{nombreCompleto}</strong>,</p>
                            <p>Adjunto encontrarás tu comprobante de compra en formato PDF. Este documento sirve como constancia oficial de tu adquisición en Marimon Autopartes.</p>
                            <p>Si tienes alguna pregunta sobre tu compra, no dudes en contactarnos respondiendo a este correo o llamando a nuestro servicio de atención al cliente.</p>
                            <p>¡Gracias por confiar en nosotros!</p>
                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #777; font-size: 0.9em;'>
                                <p>Marimon Autopartes</p>
                                <p>Teléfono: (01) 123-4567</p>
                                <p>Email: ventas@marimonperu.com</p>
                                <p>Web: <a href='https://marimonperu.com'>marimonperu.com</a></p>
                            </div>
                        </div>
                    </body>
                    </html>";

                    // Aquí está el cambio: utilizamos el método con soporte para adjuntos
                    await _emailSender.SendEmailWithAttachmentAsync(
                        user.Email,
                        $"Tu {tipoComprobante} de compra - Marimon Autopartes",
                        emailHtmlBody,
                        pdfBytes,
                        $"{tipoComprobante.ToLower()}_{comprobante.com_id}.pdf",
                        "application/pdf"
                    );
                    
                    _logger.LogInformation($"Comprobante enviado por correo a {user.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al enviar el comprobante por correo: {ex.Message}");
                    // Continuar aunque falle el envío
                }
            }
            else
            {
                _logger.LogWarning($"No se pudo enviar el comprobante por correo. Usuario ID: {usuario.usu_id}");
            }

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
    <!DOCTYPE html>
    <html lang='es'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Comprobante Electrónico - Marimon</title>
        <style>
            /* Paleta de colores y variables */
            :root {{
                --primary-red: #e62020;
                --red-hover: #c51818;
                --red-light: #ff6b6b;
                --dark-black: #121212;
                --gray-dark: #333333;
                --gray-medium: #777777;
                --gray-light: #f1f1f1;
                --white: #ffffff;
            }}
            
            /* Estilos generales */
            * {{
                margin: 0;
                padding: 0;
                box-sizing: border-box;
            }}
            
            body {{
                font-family: 'Helvetica Neue', Arial, sans-serif;
                color: var(--dark-black);
                background-color: var(--white);
                line-height: 1.6;
                -webkit-font-smoothing: antialiased;
            }}
            
            /* Contenedor principal */
            .receipt-container {{
                max-width: 800px;
                margin: 0 auto;
                background: var(--white);
                border-radius: 8px;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15);
                overflow: hidden;
                position: relative;
            }}
            
            /* Cinta roja superior */
            .top-ribbon {{
                height: 15px;
                background: linear-gradient(90deg, var(--primary-red) 0%, var(--red-light) 50%, var(--primary-red) 100%);
                margin-bottom: 0;
            }}
            
            /* Encabezado */
            .receipt-header {{
                background-color: var(--dark-black);
                color: var(--white);
                padding: 25px 30px;
                display: flex;
                justify-content: space-between;
                align-items: center;
                position: relative;
            }}
            
            /* Contenedor del logo */
            .logo-container {{
                display: flex;
                align-items: center;
            }}
            
            .logo-container img {{
                height: 60px;
                width: auto;
                margin-right: 15px;
                filter: drop-shadow(0 2px 4px rgba(0,0,0,0.2));
            }}
            
            .company-name {{
                font-size: 26px;
                font-weight: 800;
                text-transform: uppercase;
                letter-spacing: 2px;
                color: var(--white);
                text-shadow: 1px 1px 2px rgba(0,0,0,0.3);
            }}
            
            /* Título del comprobante */
            .receipt-title {{
                font-size: 20px;
                font-weight: 700;
                background-color: var(--primary-red);
                color: var(--white);
                padding: 10px 20px;
                border-radius: 4px;
                box-shadow: 0 4px 8px rgba(0,0,0,0.2);
                text-transform: uppercase;
                letter-spacing: 1px;
                margin-left: 20px;
            }}
            
            /* Contenido principal */
            .receipt-content {{
                padding: 30px;
            }}
            
            /* Información de comprobante */
            .receipt-info {{
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 30px;
                margin-bottom: 30px;
                background-color: var(--gray-light);
                padding: 20px;
                border-radius: 8px;
                position: relative;
            }}
            
            .receipt-info::before {{
                content: '';
                position: absolute;
                left: 0;
                top: 0;
                height: 100%;
                width: 6px;
                background-color: var(--primary-red);
                border-top-left-radius: 8px;
                border-bottom-left-radius: 8px;
            }}
            
            .receipt-info-column {{
                display: flex;
                flex-direction: column;
                gap: 12px;
            }}
            
            .receipt-info-item {{
                display: flex;
                align-items: baseline;
                font-size: 15px;
            }}
            
            .receipt-info-item strong {{
                color: var(--primary-red);
                font-weight: 600;
                min-width: 180px;
                display: inline-block;
            }}
            
            /* Sección de detalles de compra */
            .purchase-details {{
                margin: 30px 0;
            }}
            
            .section-title {{
                font-size: 20px;
                color: var(--dark-black);
                margin-bottom: 20px;
                border-bottom: 3px solid var(--primary-red);
                padding-bottom: 8px;
                display: inline-block;
                position: relative;
            }}
            
            /* Tabla de productos */
            table {{
                width: 100%;
                border-collapse: collapse;
                margin: 20px 0;
                border-radius: 8px;
                overflow: hidden;
                box-shadow: 0 4px 10px rgba(0,0,0,0.1);
                font-size: 15px;
            }}
            
            thead {{
                background: linear-gradient(90deg, var(--dark-black) 0%, var(--gray-dark) 100%);
                color: var(--white);
            }}
            
            th {{
                padding: 15px;
                text-align: left;
                font-weight: 600;
                text-transform: uppercase;
                font-size: 14px;
                letter-spacing: 0.5px;
            }}
            
            td {{
                padding: 15px;
                border-bottom: 1px solid var(--gray-light);
            }}
            
            tr:nth-child(even) {{
                background-color: var(--gray-light);
            }}
            
            tr:hover {{
                background-color: rgba(230, 32, 32, 0.05);
            }}
            
            /* Fila de total */
            .total-row {{
                font-weight: bold;
                font-size: 18px;
                background-color: var(--white) !important;
            }}
            
            .total-row td {{
                padding: 20px 15px;
                border-top: 2px solid var(--primary-red);
                border-bottom: none;
            }}
            
            .total-row .total-label {{
                text-align: right;
                color: var(--dark-black);
            }}
            
            .total-row .total-value {{
                color: var(--primary-red);
                font-size: 20px;
                font-weight: 700;
            }}
            
            /* Pie de página */
            .receipt-footer {{
                margin-top: 40px;
                padding: 25px 30px;
                text-align: center;
                background-color: var(--gray-light);
                color: var(--gray-medium);
                font-size: 14px;
                border-top: 1px solid #ddd;
                position: relative;
            }}
            
            /* Sello y códigos */
            .receipt-stamp {{
                text-align: center;
                margin: 25px 0;
                opacity: 0.6;
            }}
            
            .barcode {{
                text-align: center;
                margin: 30px 0 15px;
            }}
            
            .barcode-img {{
                max-width: 90%;
                height: 40px;
                filter: grayscale(100%);
                opacity: 0.8;
            }}
            
            .document-id {{
                text-align: center;
                font-size: 12px;
                color: var(--gray-medium);
                letter-spacing: 1px;
                font-family: monospace;
            }}
            
            /* Cinta roja inferior */
            .bottom-ribbon {{
                height: 15px;
                background: linear-gradient(90deg, var(--primary-red) 0%, var(--red-light) 50%, var(--primary-red) 100%);
                margin-top: 0;
            }}
            
            /* Estilos de impresión */
            @media print {{
                body {{
                    background-color: var(--white);
                }}
                
                .receipt-container {{
                    box-shadow: none;
                    border: none;
                    max-width: 100%;
                }}
                
                .top-ribbon, .bottom-ribbon {{
                    -webkit-print-color-adjust: exact;
                    print-color-adjust: exact;
                }}
                
                table {{
                    box-shadow: none;
                }}
            }}
        </style>
    </head>
    <body>
        <div class='receipt-container'>
            <div class='top-ribbon'></div>
            
            <div class='receipt-header'>
                <div class='logo-container'>
                    <img src='https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png' />
                    <div class='company-name'></div>
                </div>
                <div class='receipt-title'>Comprobante Electrónico</div>
            </div>
            
            <div class='receipt-content'>
                <div class='receipt-info'>
                    <div class='receipt-info-column'>
                        <div class='receipt-info-item'><strong>Tipo de comprobante:</strong> {comprobanteCompleto.tipo_comprobante}</div>
                        <div class='receipt-info-item'><strong>Fecha:</strong> {venta.ven_fecha}</div>
                        <div class='receipt-info-item'><strong>Venta ID:</strong> {venta.ven_id}</div>
                    </div>
                    <div class='receipt-info-column'>";

            // Datos específicos según tipo
            if (comprobanteCompleto.tipo_comprobante.ToLower() == "boleta" && comprobanteCompleto.Boletas.Any())
            {
                var boleta = comprobanteCompleto.Boletas.First();
                htmlContent += $@"
                        <div class='receipt-info-item'><strong>ID Boleta:</strong> {boleta.bol_id}</div>
                        <div class='receipt-info-item'><strong>Número de Identificación:</strong> {boleta.num_identificacion}</div>";
            }
            else if (comprobanteCompleto.tipo_comprobante.ToLower() == "factura" && comprobanteCompleto.Facturas.Any())
            {
                var factura = comprobanteCompleto.Facturas.First();
                htmlContent += $@"
                        <div class='receipt-info-item'><strong>Razón Social:</strong> {factura.fac_razonsocial}</div>
                        <div class='receipt-info-item'><strong>RUC:</strong> {factura.fac_ruc}</div>
                        <div class='receipt-info-item'><strong>Dirección:</strong> {factura.fac_direccion}</div>";
            }

            htmlContent += $@"
                    </div>
                </div>
                
                <div class='purchase-details'>
                    <h3 class='section-title'>Detalles de la compra</h3>
                    <table>
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
                            <tr class='total-row'>
                                <td colspan='3' class='total-label'><strong>Total:</strong></td>
                                <td class='total-value'>{venta.Total:C}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>               
                
                <div class='receipt-footer'>
                    Este comprobante es generado electrónicamente y no requiere firma o sello.
                    <div class='receipt-stamp'>
                        MARIMON AUTOPARTES © {DateTime.Now.Year}
                    </div>
                </div>
            </div>
            
            <div class='bottom-ribbon'></div>
        </div>
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