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
using System.Drawing;
using System.IO;
using Microsoft.Extensions.Options;
using System.Globalization;
using Stripe;
using Stripe.Checkout;
using System.Security.Cryptography;
using System.Text;

namespace Marimon.Controllers
{
    public class ComprobanteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComprobanteController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly StripeSettings _stripeSettings;

        private readonly IConverter _converter;

        private readonly IEmailSenderWithAttachments _emailSender; // Cambio aquí



        public ComprobanteController(ApplicationDbContext context,
                                    ILogger<ComprobanteController> logger,
                                    UserManager<IdentityUser> userManager,
                                    IConverter converter,
                                    IEmailSenderWithAttachments emailSender,
                                    IOptions<StripeSettings> stripeSettings)
        {
            _converter = converter;
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
            _stripeSettings = stripeSettings.Value;
        }

// MODIFICACIONES EN EL MÉTODO Index (agregar parámetro para código de descuento)
public async Task<IActionResult> Index(string codigoDescuento = null)
{
    var identityUserId = _userManager.GetUserId(User);
    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);

    if (usuario == null)
    {
        return RedirectToAction("Login", "Account");
    }

    var carrito = await _context.Carritos
        .Include(c => c.CarritoAutopartes)
            .ThenInclude(ca => ca.Autoparte)
                .ThenInclude(a => a.Ofertas) // Incluye las ofertas
        .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

    if (carrito == null || !carrito.CarritoAutopartes.Any())
    {
        return RedirectToAction("Index", "Carrito", new { mensaje = "Tu carrito está vacío" });
    }

    // Convertir los elementos del carrito a CarritoItem con precios actualizados
    var carritoItems = carrito.CarritoAutopartes.Select(item =>
    {
        var autoparte = item.Autoparte;
        decimal precioUnitario = CalcularPrecioAutopartes(autoparte, codigoDescuento);

        return new Carrito.CarritoItem
        {
            Nombre = autoparte.aut_nombre,
            PrecioUnitario = precioUnitario,
            Cantidad = item.car_cantidad,
            ImagenUrl = autoparte.aut_imagen
        };
    }).ToList();

    ViewBag.TipoComprobante = TempData["tipoComprobante"]?.ToString();
    ViewBag.TipoDocumento = TempData["tipoDocumento"]?.ToString();
    ViewBag.NumIdentificacion = TempData["num_identificacion"]?.ToString();
    ViewBag.FacRazon = TempData["fac_razon"]?.ToString();
    ViewBag.FacRuc = TempData["fac_ruc"]?.ToString();
    ViewBag.FacDireccion = TempData["fac_direccion"]?.ToString();
    ViewBag.CodigoDescuento = codigoDescuento; // Pasar el código a la vista
    

    var modelo = new Tuple<Usuario, List<Carrito.CarritoItem>>(usuario, carritoItems);

    return View(modelo);
}


// MÉTODO PARA CALCULAR PRECIO CON OFERTAS Y DESCUENTOS
private decimal CalcularPrecioAutopartes(Autoparte autoparte, string codigoDescuento = null)
{
    decimal precio = autoparte.aut_precio;
    
    // Primero verificar si hay oferta activa
    var ofertaActiva = autoparte.Ofertas?.FirstOrDefault(o => 
        o.ofe_activa && 
        o.ofe_fecha_inicio <= DateTime.Now && 
        o.ofe_fecha_fin >= DateTime.Now);
        
    if (ofertaActiva != null)
    {
        precio = autoparte.aut_precio * (1 - ofertaActiva.ofe_porcentaje / 100);
    }
    
    // Si hay código de descuento, verificar si aplica
    if (!string.IsNullOrEmpty(codigoDescuento))
    {
        var codigo = _context.CodigosDescuento.FirstOrDefault(c => 
            c.cod_codigo == codigoDescuento && 
            !c.cod_utilizado &&
            c.cod_fecha_expiracion >= DateTime.Now &&
            (c.AutoparteId == null || c.AutoparteId == autoparte.aut_id));
            
        if (codigo != null)
        {
            // Aplicar descuento adicional al precio (que ya puede tener oferta aplicada)
            precio = precio * (1 - codigo.cod_porcentaje / 100);
        }
    }
    
    return precio;
}

// MÉTODO PARA CALCULAR TOTAL DEL CARRITO CON OFERTAS Y DESCUENTOS
private decimal CalcularTotalAutopartes(List<CarritoAutoparte> items, string codigoDescuento = null)
{
    decimal total = 0;
    foreach (var item in items)
    {
        decimal precioFinal = CalcularPrecioAutopartes(item.Autoparte, codigoDescuento);
        total += precioFinal * item.car_cantidad;
    }
    return total;
}

// MÉTODO PARA MARCAR CÓDIGO DE DESCUENTO COMO UTILIZADO
private async Task MarcarCodigoComoUtilizadoAsync(string codigoDescuento)
{
    if (!string.IsNullOrEmpty(codigoDescuento))
    {
        var codigo = await _context.CodigosDescuento.FirstOrDefaultAsync(c => 
            c.cod_codigo == codigoDescuento && 
            !c.cod_utilizado);
            
        if (codigo != null)
        {
            codigo.cod_utilizado = true;
            _context.CodigosDescuento.Update(codigo);
        }
    }
}


        [HttpPost]
        public async Task<IActionResult> RegistrarComprobante(string tipoComprobante, string num_identificacion, string fac_razon, string fac_ruc, string fac_direccion,string codigoDescuento = null)
        {
            if (string.IsNullOrEmpty(codigoDescuento) && TempData["codigoDescuento"] != null)
            {
                codigoDescuento = TempData["codigoDescuento"].ToString();
            }

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
                Total = carrito.car_total,
                CodigoDescuento = codigoDescuento // <--- aquí lo guardas
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

            // RUTA DEL PDF GENERADO
            var rutaPdf = SavePdfToFile(pdfBytes, comprobante.com_id, tipoComprobante);

            // Actualizar la tabla Comprobante con la ruta del PDF
            comprobante.com_imagen = rutaPdf;
            _context.Update(comprobante);
            await _context.SaveChangesAsync();

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


        //STRIPE
[HttpPost]
public async Task<IActionResult> ProcesarPago(string tipoComprobante, string tipoDocumento, string metodoPago, 
    string num_identificacion, string fac_razon, string fac_ruc, string fac_direccion, string codigoDescuento = null)
{
    TempData["tipoComprobante"] = tipoComprobante;
    TempData["tipoDocumento"] = tipoDocumento;
    TempData["num_identificacion"] = num_identificacion;
    TempData["fac_razon"] = fac_razon;
    TempData["fac_ruc"] = fac_ruc;
    TempData["fac_direccion"] = fac_direccion;
    TempData["codigoDescuento"] = codigoDescuento;

    var identityUserId = _userManager.GetUserId(User);
    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);

    if (usuario == null)
    {
        return Unauthorized("Usuario no encontrado.");
    }

    // IMPORTANTE: Incluir las ofertas en la consulta
    var carrito = await _context.Carritos
        .Include(c => c.CarritoAutopartes)
            .ThenInclude(cp => cp.Autoparte)
                .ThenInclude(a => a.Ofertas) // AGREGAR ESTA LÍNEA
        .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

    if (carrito == null || !carrito.CarritoAutopartes.Any())
    {
        return BadRequest("El carrito está vacío.");
    }

    // Si el método de pago es Yape
    if (metodoPago == "yape")
    {
        // Calcular total con ofertas y descuentos para Yape
        decimal totalAutopartesYape = CalcularTotalAutopartes(carrito.CarritoAutopartes.ToList(), codigoDescuento);
        
        TempData["tipoComprobante"] = tipoComprobante;
        TempData["tipoDocumento"] = tipoDocumento;
        TempData["num_identificacion"] = num_identificacion;
        TempData["fac_razon"] = fac_razon;
        TempData["fac_ruc"] = fac_ruc;
        TempData["fac_direccion"] = fac_direccion;
        TempData["codigoDescuento"] = codigoDescuento;
        TempData["totalPago"] = totalAutopartesYape.ToString(CultureInfo.InvariantCulture);


        return RedirectToAction("PagoYape");
    }

    // Para pagos con tarjeta - Calcular total con ofertas y descuentos
    decimal totalAutopartes = CalcularTotalAutopartes(carrito.CarritoAutopartes.ToList(), codigoDescuento);

    // Crear venta temporal con total correcto
    var venta = new Venta
    {
        ven_fecha = DateOnly.FromDateTime(DateTime.Now),
        UsuarioId = usuario.usu_id,
        MetodoPagoId = metodoPago == "tarjeta" ? 2 : 1,
        Estado = "Completado",
        Total = totalAutopartes, // Usar total calculado
        CodigoDescuento = codigoDescuento // <--- aquí lo guardas
    };

    _context.Venta.Add(venta);
    await _context.SaveChangesAsync();

    // Marcar código de descuento como utilizado si existe
    await MarcarCodigoComoUtilizadoAsync(codigoDescuento);

    // Guardar datos de comprobante temporalmente
    TempData["tipoComprobante"] = tipoComprobante;
    TempData["num_identificacion"] = num_identificacion;
    TempData["fac_razon"] = fac_razon;
    TempData["fac_ruc"] = fac_ruc;
    TempData["fac_direccion"] = fac_direccion;
    TempData["codigoDescuento"] = codigoDescuento;
    TempData["ventaId"] = venta.ven_id;

    // Crear sesión de Stripe con precios correctos
    var options = new SessionCreateOptions
    {
        PaymentMethodTypes = new List<string> { "card" },
        LineItems = carrito.CarritoAutopartes.Select(item =>
        {
            var autoparte = item.Autoparte;
            decimal precioFinal = CalcularPrecioAutopartes(autoparte, codigoDescuento);
            
            return new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "pen",
                    UnitAmount = (long)(precioFinal * 100), // Usar precio final
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = autoparte.aut_nombre,
                        Description = autoparte.aut_descripcion,
                        Images = new List<string> { autoparte.aut_imagen }
                    },
                },
                Quantity = item.car_cantidad,
            };
        }).ToList(),
        Mode = "payment",
        SuccessUrl = $"{Request.Scheme}://{Request.Host}/Comprobante/PagoExitoso?session_id={{CHECKOUT_SESSION_ID}}",
        CancelUrl = Url.Action("Index", "Comprobante", null, Request.Scheme)
    };

    var service = new SessionService();
    var session = service.Create(options);

    venta.StripeSessionId = session.Id;
    await _context.SaveChangesAsync();

    return Redirect(session.Url);
}
        private async Task EnviarCorreoEstadoAsync(int ventaId, string estado, string correoUsuario, string nombreUsuario, [FromServices] IEmailSenderWithAttachments emailSender)
        {
            try
            {
                // Cargar la plantilla de correo
                var emailTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Emails", "EstadosPedido.html");
                var emailBody = await System.IO.File.ReadAllTextAsync(emailTemplatePath);

                // Definir clases CSS según el estado
                string stylePendiente = "opacity:0.4; color:#F39C12; font-weight:bold; font-size:14px; padding:0 15px;";
                string styleCompletado = "opacity:0.4; color:#27ae60; font-weight:bold; font-size:14px; padding:0 15px;";
                string styleCancelado = "opacity:0.4; color:#E74C3C; font-weight:bold; font-size:14px; padding:0 15px;";
                string claseEstadoTexto = "";
                string colCancelado = "";
                string mensajeCambio = $"El estado de tu pedido <strong>#{ventaId}</strong> esta";
                string mensajeEstado = "";

                if (estado == "Pendiente")
                {
                    stylePendiente = "opacity:1; color:#F39C12; font-weight:bold; font-size:14px; padding:0 15px;";
                    mensajeEstado = "Tu pedido está pendiente de confirmación. Estamos revisando tu pago y te notificaremos cuando se confirme.";
                    claseEstadoTexto = "color: #F39C12;";
                }
                else if (estado == "Completado")
                {
                    styleCompletado = "opacity:1; color:#27ae60; font-weight:bold; font-size:14px; padding:0 15px;";
                    claseEstadoTexto = "color: #27ae60;";
                    mensajeEstado = "¡Tu pedido ha sido completado exitosamente! Ya puedes acercarte a recogerlo en el local.";
                }
                else if (estado == "Cancelado")
                {
                    colCancelado = @"<td style=""opacity:1; color:#E74C3C; font-weight:bold; font-size:14px; padding:0 15px;"">
                        <img src=""https://firebasestorage.googleapis.com/v0/b/marimonapp.appspot.com/o/Assest_web%2FPedidos%2Fel-tiempo-de-entrega.png?alt=media&token=3eed4a09-a993-40fc-9989-4abcc4c2359b.jpg""
                            alt=""Cancelado"" width=""50"" height=""50"" style=""display:block; margin:0 auto 10px auto;"">
                        Cancelado
                    </td>";
                    claseEstadoTexto = "color: #E42229;";
                    mensajeEstado = "Tu pedido ha sido cancelado. Si tienes dudas, por favor contáctanos para más información.";
                }

                emailBody = emailBody.Replace("{{UserName}}", nombreUsuario ?? "Cliente")
                                        .Replace("{{PedidoId}}", ventaId.ToString())
                                        .Replace("{{Estado}}", estado)
                                        .Replace("{{LogoUrl}}", "https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png")
                                        .Replace("{{CallbackUrl}}", "https://marimon-fjv7.onrender.com/Identity/Account/Manage/Pedidos")
                                        .Replace("{{StylePendiente}}", stylePendiente)
                                        .Replace("{{StyleCompletado}}", styleCompletado)
                                        .Replace("{{StyleCancelado}}", styleCancelado)
                                        .Replace("{{ColCancelado}}", colCancelado)
                                        .Replace("{{MensajeEstado}}", mensajeEstado)
                                        .Replace("{{MensajeCambio}}", mensajeCambio)
                                        .Replace("{{ClaseEstadoTexto}}", claseEstadoTexto);


                var subject = $"Estado de tu pedido #{ventaId} actualizado a {estado}";

                // Enviar el correo
                await emailSender.SendEmailAsync(correoUsuario, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al enviar el correo de estado: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> PagoExitoso(string session_id)
        {
            // Verificar el estado del pago
            var service = new SessionService();
            var session = service.Get(session_id);

            // Buscar la venta asociada a esta sesión de Stripe
            var venta = await _context.Venta.FirstOrDefaultAsync(v => v.StripeSessionId == session_id);

            if (venta == null)
            {
                return NotFound("No se encontró la venta asociada a esta sesión de pago.");
            }

            var comprobante = await _context.Comprobante
               .Include(c => c.Boletas)
               .Include(c => c.Facturas)
               .FirstOrDefaultAsync(c => c.VentaId == venta.ven_id);

            if (session.PaymentStatus == "paid")
            {
                if (comprobante != null)
                {
                    return View("PagoExitoso", comprobante);
                }
                // El pago fue exitoso, continuar con el proceso de registro de comprobante
                string tipoComprobante = TempData["tipoComprobante"]?.ToString();
                string num_identificacion = TempData["num_identificacion"]?.ToString();
                string fac_razon = TempData["fac_razon"]?.ToString();
                string fac_ruc = TempData["fac_ruc"]?.ToString();
                string fac_direccion = TempData["fac_direccion"]?.ToString();

                // Aquí reutilizamos gran parte de la lógica de RegistrarComprobante
                // Crear el comprobante
                comprobante = new Comprobante
                {
                    tipo_comprobante = tipoComprobante,
                    VentaId = venta.ven_id,
                    com_evidencia = string.Empty
                };
                _context.Comprobante.Add(comprobante);
                await _context.SaveChangesAsync();

                // Obtener el carrito y crear salidas
                var carrito = await _context.Carritos
                    .Include(c => c.CarritoAutopartes)
                        .ThenInclude(cp => cp.Autoparte)
                    .FirstOrDefaultAsync(c => c.UsuarioId == venta.UsuarioId);

                // Crear detalles de venta y salidas
                foreach (var item in carrito.CarritoAutopartes)
                {
                    var detalle = new DetalleVentas
                    {
                        VentaId = venta.ven_id,
                        AutoParteId = item.AutoparteId,
                        det_cantidad = item.car_cantidad
                    };
                    _context.DetalleVentas.Add(detalle);

                    // Reducir stock
                    var autoparte = await _context.Autopartes.FirstOrDefaultAsync(a => a.aut_id == item.AutoparteId);
                    if (autoparte != null)
                    {
                        autoparte.aut_cantidad -= item.car_cantidad;
                        _context.Autopartes.Update(autoparte);
                    }

                    // Crear salida
                    var salida = new Salida
                    {
                        sal_fechasalida = DateOnly.FromDateTime(DateTime.Now),
                        sal_cantidad = item.car_cantidad,
                        ComprobanteId = comprobante.com_id,
                        AutoparteId = item.AutoparteId
                    };
                    _context.Salida.Add(salida);
                }

                // Crear Boleta o Factura
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

                await _context.SaveChangesAsync();

                // Limpiar el carrito
                _context.CarritoAutopartes.RemoveRange(carrito.CarritoAutopartes);
                await _context.SaveChangesAsync();

                // Generar el PDF
                var htmlContent = GenerateComprobanteHtml(comprobante);
                var pdfBytes = ConvertHtmlToPdf(htmlContent);

                // Añadir después de generar el PDF y antes de enviar el correo
                var rutaPdf = SavePdfToFile(pdfBytes, comprobante.com_id, tipoComprobante);

                // Actualizar la tabla Comprobante con la ruta del PDF
                comprobante.com_imagen = rutaPdf;
                _context.Update(comprobante);
                await _context.SaveChangesAsync();

                // Enviar por correo
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == venta.UsuarioId);
                var user = await _userManager.FindByIdAsync(usuario.usu_id);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Código para enviar el correo (igual que en RegistrarComprobante)
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

                    try
                    {
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

                // Mostrar vista de éxito
                return View("PagoExitoso", comprobante);
            }

            return View("Error", "El pago no se completó correctamente.");
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

            // Obtener el usuario para datos de cliente
            var usuario = _context.Usuarios.FirstOrDefault(u => u.usu_id == venta.UsuarioId);

            // Generar número de comprobante formatado
            string numeroComprobante = $"{(comprobanteCompleto.tipo_comprobante.ToLower() == "factura" ? "F" : "B")}001-{comprobanteCompleto.com_id.ToString("0000000")}";

            // Fecha actual formateada
            string fechaEmision = DateTime.Now.ToString("dd/MM/yyyy");

            // Calcular IGV (18%)
            decimal subtotal = Math.Round(venta.Total / 1.18m, 2);
            decimal igv = Math.Round(venta.Total - subtotal, 2);

            // Iniciar HTML para el comprobante
            var htmlContent = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Comprobante Electrónico - Marimon</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            font-size: 12px;
            line-height: 1.4;
            margin: 0;
            padding: 20px;
            color: #000;
        }}
        
        .container {{
            max-width: 800px;
            margin: 0 auto;
            border: 1px solid #000;
            padding: 15px;
        }}
        
        .header {{
            margin-bottom: 20px;
            border-bottom: 1px solid #000;
            padding-bottom: 10px;
        }}
        
        .header-content {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }}
        
        .logo-section {{
            width: 65%;
        }}
        
        .logo {{
            max-width: 180px;
            margin-bottom: 10px;
        }}
        
        .document-section {{
            width: 30%;
            text-align: center;
            border: 2px solid #000;
            padding: 10px;
        }}
        
        .document-title {{
            font-weight: bold;
            font-size: 16px;
            margin: 5px 0;
        }}
        
        .document-number {{
            font-weight: bold;
            font-size: 16px;
            margin: 10px 0;
        }}
        
        .client-info {{
            display: flex;
            margin-bottom: 20px;
        }}
        
        .client-info-label {{
            font-weight: bold;
            width: 150px;
        }}
        
        .invoice-meta {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }}
        
        .invoice-meta td {{
            border: 1px solid #000;
            padding: 5px 10px;
        }}
        
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }}
        
        .items-table th, .items-table td {{
            border: 1px solid #000;
            padding: 8px;
            text-align: center;
        }}
        
        .items-table th {{
            background-color: #f2f2f2;
        }}
        
        .items-table .description {{
            text-align: left;
        }}
        
        .totals-table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }}
        
        .totals-table td {{
            padding: 5px 10px;
            text-align: right;
        }}
        
        .qr-section {{
            display: flex;
            justify-content: flex-start;
            margin-top: 20px;
        }}
        
        .qr-box {{
            width: 120px;
            height: 120px;
            border: 1px solid #ccc;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        
        .qr-info {{
            margin-left: 20px;
        }}
        
        .observations {{
            margin-top: 20px;
            border-top: 1px solid #000;
            padding-top: 10px;
            font-size: 11px;
        }}
        
        .center {{
            text-align: center;
        }}
        
        .right {{
            text-align: right;
        }}
        
        .amount-in-words {{
            font-style: italic;
            margin: 10px 0;
            border-bottom: 1px solid #000;
            padding-bottom: 10px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='header-content'>
                <div class='logo-section'>
                    <img src='https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png' alt='Logo Marimon' class='logo'/>
                    <p>JR. GRAL. FELIPE SANTIAGO SALAVERRY 44, Lima 15022</p>
                    <p>Teléfono: +51 986418904</p>
                </div>
                <div class='document-section'>
                    <p>RUC: 10403771975</p>
                    <p class='document-title'>{comprobanteCompleto.tipo_comprobante.ToUpper()} ELECTRÓNICA</p>
                    <p class='document-number'>{numeroComprobante}</p>
                </div>
            </div>
        </div>
        
        <div class='client-info'>
            <div>";
            // Datos de cliente según tipo de comprobante
            if (comprobanteCompleto.tipo_comprobante.ToLower() == "factura" && comprobanteCompleto.Facturas.Any())
            {
                var factura = comprobanteCompleto.Facturas.First();
                htmlContent += $@"
                <p><span class='client-info-label'>SEÑOR(es):</span> {factura.fac_razonsocial}</p>
                <p><span class='client-info-label'>RUC No:</span> {factura.fac_ruc}</p>
                <p><span class='client-info-label'>RAZÓN SOCIAL:</span> {factura.fac_razonsocial}</p>
                <p><span class='client-info-label'>DIRECCIÓN:</span> {factura.fac_direccion}</p>";
            }
            else if (comprobanteCompleto.tipo_comprobante.ToLower() == "boleta" && comprobanteCompleto.Boletas.Any())
            {
                var boleta = comprobanteCompleto.Boletas.First();
                htmlContent += $@"
                <p><span class='client-info-label'>SEÑOR(es):</span> {usuario?.usu_nombre} {usuario?.usu_apellido}</p>
                <p><span class='client-info-label'>DNI No:</span> {boleta.num_identificacion}</p>
                ";
            }

            htmlContent += $@"
            </div>
        </div>
        
        <table class='invoice-meta'>
            <tr>
                <td>Nro INTERNO</td>
                <td>FECHA EMISIÓN</td>
                <td>FECHA VENCIMIENTO</td>
                <td>CONDICIONES</td>
                <td>GUÍA REMISIÓN</td>
            </tr>
            <tr>
                <td>{venta.ven_id}</td>
                <td>{fechaEmision}</td>
                <td>{fechaEmision}</td>
                <td>CONTADO</td>
                <td></td>
            </tr>
        </table>
        
        <table class='items-table'>
            <thead>
                <tr>
                    <th>Cant.</th>
                    <th>Código</th>
                    <th>Descripción</th>
                    <th>Pre. Unit.</th>
                    <th>Sub Total</th>
                    <th>Total</th>
                </tr>
            </thead>
            <tbody>";
            foreach (var detalle in detallesVenta)
            {
                var autoparte = detalle.Autoparte;
                if (autoparte == null) continue;

                decimal precioBase = autoparte.aut_precio;
                decimal precioFinal = precioBase;

                // Buscar oferta activa
                var ofertaActiva = _context.Ofertas.FirstOrDefault(o =>
                    o.AutoparteId == autoparte.aut_id &&
                    o.ofe_activa &&
                    DateTime.Now >= o.ofe_fecha_inicio &&
                    DateTime.Now <= o.ofe_fecha_fin
                );

                // Si hay oferta, aplicar descuento
                if (ofertaActiva != null)
                {
                    precioFinal = Math.Round(precioBase * (1 - ofertaActiva.ofe_porcentaje / 100), 2);
                }
                // Si no hay oferta activa, verificar si se aplicó un código de descuento a la venta
                else if (!string.IsNullOrEmpty(venta.CodigoDescuento))
                {
                    // Buscar el código de descuento que se aplicó a esta venta
                    var codigoDescuentoAplicado = _context.CodigosDescuento.FirstOrDefault(cd =>
                        cd.cod_codigo == venta.CodigoDescuento &&
                        (cd.AutoparteId == null || cd.AutoparteId == autoparte.aut_id)
                    );

                    if (codigoDescuentoAplicado != null)
                    {
                        precioFinal = Math.Round(precioBase * (1 - codigoDescuentoAplicado.cod_porcentaje / 100), 2);
                    }
                }

                // Calcular subtotales
                int cantidad = detalle.det_cantidad;
                decimal subTotalItem = Math.Round(precioFinal * cantidad, 2);
                decimal totalItem = subTotalItem;

                htmlContent += $@"
                <tr>
                    <td>{cantidad}</td>
                    <td>{autoparte.aut_id}</td>
                    <td class='description'>{autoparte.aut_nombre}</td>
                    <td class='right'>{precioFinal:N2}</td>
                    <td class='right'>{subTotalItem:N2}</td>
                    <td class='right'>{totalItem:N2}</td>
                </tr>";
            }

            // Convertir a palabras (ejemplo básico)
            string montoEnPalabras = ConvertirNumeroALetras((double)venta.Total) + " CON " +
                                     ((int)((venta.Total - Math.Truncate(venta.Total)) * 100)).ToString("00") + "/100 Soles";

            htmlContent += $@"
            </tbody>
        </table>
        
        <p class='amount-in-words'>SON: {montoEnPalabras}</p>
        
        <table class='totals-table'>
            <tr>
                <td>SUB TOTAL</td>
                <td>S/</td>
                <td>{venta.Total:N2}</td>
            </tr>
          
            <tr>
                <td><strong>IMPORTE TOTAL</strong></td>
                <td><strong>S/</strong></td>
                <td><strong>{venta.Total:N2}</strong></td>
            </tr>
        </table>
        
        <div class='qr-section'>
            <div style='flex-grow: 1; padding-left: 20px;'>
                <p style='margin-top: 0;'><strong>Representación impresa de la {comprobanteCompleto.tipo_comprobante} electrónica</strong></p>
                <p>Autorizado mediante resolución Nro:</p>
            </div>
        </div>";
            var hash = GenerarHash(comprobanteCompleto.Venta.StripeSessionId ?? comprobanteCompleto.Venta.ven_id.ToString());
            htmlContent += $@"
        <div class='observations'>
            El {comprobanteCompleto.tipo_comprobante} numero {numeroComprobante}, ha sido aceptada | Hash : {hash}
        </div>
    </div>
</body>
</html>";

            return htmlContent;
        }

        /// <summary>
        /// Convierte un número a su expresión en letras
        /// </summary>
        private string ConvertirNumeroALetras(double valor)
        {
            string Num2Text = "";
            valor = Math.Truncate(valor);
            if (valor == 0) Num2Text = "CERO";
            else if (valor == 1) Num2Text = "UNO";
            else if (valor == 2) Num2Text = "DOS";
            else if (valor == 3) Num2Text = "TRES";
            else if (valor == 4) Num2Text = "CUATRO";
            else if (valor == 5) Num2Text = "CINCO";
            else if (valor == 6) Num2Text = "SEIS";
            else if (valor == 7) Num2Text = "SIETE";
            else if (valor == 8) Num2Text = "OCHO";
            else if (valor == 9) Num2Text = "NUEVE";
            else if (valor == 10) Num2Text = "DIEZ";
            else if (valor == 11) Num2Text = "ONCE";
            else if (valor == 12) Num2Text = "DOCE";
            else if (valor == 13) Num2Text = "TRECE";
            else if (valor == 14) Num2Text = "CATORCE";
            else if (valor == 15) Num2Text = "QUINCE";
            else if (valor < 20) Num2Text = "DIECI" + ConvertirNumeroALetras(valor - 10);
            else if (valor == 20) Num2Text = "VEINTE";
            else if (valor < 30) Num2Text = "VEINTI" + ConvertirNumeroALetras(valor - 20);
            else if (valor == 30) Num2Text = "TREINTA";
            else if (valor == 40) Num2Text = "CUARENTA";
            else if (valor == 50) Num2Text = "CINCUENTA";
            else if (valor == 60) Num2Text = "SESENTA";
            else if (valor == 70) Num2Text = "SETENTA";
            else if (valor == 80) Num2Text = "OCHENTA";
            else if (valor == 90) Num2Text = "NOVENTA";
            else if (valor < 100)
            {
                Num2Text = ConvertirNumeroALetras(Math.Truncate(valor / 10) * 10) + " Y " + ConvertirNumeroALetras(valor % 10);
            }
            else if (valor == 100) Num2Text = "CIEN";
            else if (valor < 200) Num2Text = "CIENTO " + ConvertirNumeroALetras(valor - 100);
            else if ((valor == 200) || (valor == 300) || (valor == 400) || (valor == 600) || (valor == 800))
                Num2Text = ConvertirNumeroALetras(Math.Truncate(valor / 100)) + "CIENTOS";
            else if (valor == 500) Num2Text = "QUINIENTOS";
            else if (valor == 700) Num2Text = "SETECIENTOS";
            else if (valor == 900) Num2Text = "NOVECIENTOS";
            else if (valor < 1000)
                Num2Text = ConvertirNumeroALetras(Math.Truncate(valor / 100) * 100) + " " + ConvertirNumeroALetras(valor % 100);
            else if (valor == 1000) Num2Text = "MIL";
            else if (valor < 2000) Num2Text = "MIL " + ConvertirNumeroALetras(valor % 1000);
            else if (valor < 1000000)
            {
                Num2Text = ConvertirNumeroALetras(Math.Truncate(valor / 1000)) + " MIL";
                if ((valor % 1000) > 0) Num2Text += " " + ConvertirNumeroALetras(valor % 1000);
            }
            else if (valor == 1000000) Num2Text = "UN MILLON";
            else if (valor < 2000000) Num2Text = "UN MILLON " + ConvertirNumeroALetras(valor % 1000000);
            else if (valor < 1000000000000)
            {
                Num2Text = ConvertirNumeroALetras(Math.Truncate(valor / 1000000)) + " MILLONES";
                if ((valor % 1000000) > 0) Num2Text += " " + ConvertirNumeroALetras(valor % 1000000);
            }
            return Num2Text;
        }

        /// <summary>
        /// Genera un código QR para el comprobante
        /// </summary>
        private string GenerateQRCode(Comprobante comprobante, Venta venta)
        {
            // Nota: Para implementar esto correctamente, necesitarás agregar la librería QRCoder a tu proyecto
            // Instalar vía NuGet: Install-Package QRCoder

            try
            {
                // Datos que irán en el QR según formato SUNAT
                string qrData = $"10403771975|" +  // RUC de la empresa
                              $"{comprobante.tipo_comprobante}|" +  // Tipo de documento
                              $"F001-{comprobante.com_id.ToString("0000000")}|" +  // Número de comprobante
                              $"{venta.Total}|" +  // Monto total
                              $"{DateTime.Now.ToString("yyyy-MM-dd")}";  // Fecha de emisión

                // Si tienes la librería QRCoder, descomenta este código:
                /*
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                using (Bitmap qrBitmap = qrCode.GetGraphic(20))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] byteImage = ms.ToArray();
                        return "data:image/png;base64," + Convert.ToBase64String(byteImage);
                    }
                }
                */

                // Mientras tanto, usamos un QR estático de ejemplo
                return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAHQAAAB0CAYAAABUmhYnAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAO2SURBVHhe7ZzdbQIxEEa9QAE0QB9UQB/0QR80QSMIKXs5S/ZHiJKJDYxn7Mv3SMdK9ica3tmVj92c4HQ6fS2qqmrRNJQWTUNp0TSUFk1DadE0lBZNQ2nRNJQWTUNp0TSUFk1DadE0lBZNQ2nRNJQWTUNp0TSUFk1DadE0lBZNQ2nRNJQWTUNp0TSUFk1DadE0lBZNQ2nRNJQWTUNp0TSUFk1DadE0lBZNQ2nRNJSSod+fJw/y9PJ4Rr38eJGsofbwuJLhQTp/KRwvFBxaJXm9pfoJkDS0DZzPLxAcPIgHlsYG2wObNDTK78AXCvzVnARPGhrl1+cNCLfzCnSZuXMz6XK7wBcLDi1gZWA4NlS53ZcpGTq4XG4XdwsO3o8NVm6XlzK0PrhebnS5zyvxo9wuL2XowHK5UWAvsGxomJDlShn6d7nR34fHNFdC0tDB7XKj+NHupkO5f7OShg6cLnfvgUlDB6fL3XNgylDBcblRlrjcPQcmDRX2HnjXcncdmDZU2HrgnQfeeWDaUGHrgXceuPGgbKiw5cA7D9x8UNpQ4daDdh649aDCpqHCrQduO2jHQZVNQ4VbD7zzwF0HFTYNFbYcuPOgnQdVNg8Vthx414G7DipsGipcc+C+g+66vP+w5gkMQ4VrD7z7wGua8LRmCRxDhb0G3tqEpzVL4BgqXHvgEQOvbcLTmiVwDBU8Bx75N1h7WrMEnqGC58AjBl7ThKc1S+AZKlw88MAmnK1ZAs9QwWvgUU04W7MErqHCpYGHNuFszRK4hgoeA49owdmaJfANFS4NPLAJb2uWwDdU8Bh4RAvO1iyBc6hw7sADm/C2Zgl8QwWPgUe04G3NEvwxVPAYeEQL3tYsgXOocG7goU34W7MEzqGCx8AjWvC2Zgm8QwWPgUe04G3NErwxVLh34F4t+FuzBN6hgsfAI1rwt2YZBDRU8Bi4VwsR1iyDgIYK9ww8oIUYa5ZBQEMFj4F7tRBhzTKIaKjgMXCvFiKsWQYRDRU8Bu7VQoQ1yyCkoYLHwL1aiLBmGUQ0VPAYuFcLEdYsg5CGCvcM3L+FGGuWQUhDBY+B+7cQY80yCGmo4DFw/xZirFkGMQ0VPAbu30KMNcsgpqGCx8D9W4ixZhnnhgpbDWzbQpQ1y7g0VNhqYNsWoqxZxntDha0Gtm0hypplvDdU2Gpg2xairFnGpaHCVgPbthBlzbIIaqiw1cC2LURZs6yChgoeDWzVQpw1y9JQWjQNpUXTUFo0DaVF01BaNA2lRdNQWjQNpUXTUFo0DaVF01BSjl8/cj1+fFRMvAAAAABJRU5ErkJggg==";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generando QR: {ex.Message}");
                return "";
            }
        }
        private string GenerarHash(string valor)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(valor);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
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

        // Método para guardar el PDF en disco
        private string SavePdfToFile(byte[] pdfBytes, int comprobanteId, string tipoComprobante)
        {
            // Asegúrate que la carpeta "comprobantes" exista
            string directorio = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "comprobantes");
            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            // Crear nombre del archivo y ruta
            string nombreArchivo = $"{tipoComprobante.ToLower()}_{comprobanteId}.pdf";
            string rutaArchivo = Path.Combine(directorio, nombreArchivo);

            // Guardar el archivo
            System.IO.File.WriteAllBytes(rutaArchivo, pdfBytes);

            // Devolver la ruta relativa para guardar en la BD
            return $"/comprobantes/{nombreArchivo}";
        }

        public IActionResult PagoYape()
        {
            var viewModel = new PagoYapeViewModel
            {
                TipoComprobante = TempData["tipoComprobante"]?.ToString(),
                NumIdentificacion = TempData["num_identificacion"]?.ToString(),
                RazonSocial = TempData["fac_razon"]?.ToString(),
                Ruc = TempData["fac_ruc"]?.ToString(),
                Direccion = TempData["fac_direccion"]?.ToString(),
                TotalPago = Convert.ToDecimal(TempData["totalPago"], CultureInfo.InvariantCulture)
            };

            // Mantener los datos en TempData para el siguiente request
            TempData.Keep("tipoComprobante");
            TempData.Keep("num_identificacion");
            TempData.Keep("fac_razon");
            TempData.Keep("fac_ruc");
            TempData.Keep("fac_direccion");
            TempData.Keep("totalPago");
            
            return View(viewModel);
        }

[HttpPost]
public async Task<IActionResult> ConfirmarPagoYape(IFormFile comprobanteFile)
{
    if (comprobanteFile == null || comprobanteFile.Length == 0)
    {
        return BadRequest("No se ha seleccionado ningún archivo.");
    }

    string tipoComprobante = TempData["tipoComprobante"]?.ToString();
    string num_identificacion = TempData["num_identificacion"]?.ToString();
    string fac_razon = TempData["fac_razon"]?.ToString();
    string fac_ruc = TempData["fac_ruc"]?.ToString();
    string fac_direccion = TempData["fac_direccion"]?.ToString();
    string codigoDescuento = TempData["codigoDescuento"]?.ToString();

    // Validar tipo de archivo
    string[] permitidos = { ".jpg", ".jpeg", ".png" };
    var extension = Path.GetExtension(comprobanteFile.FileName).ToLowerInvariant();

    if (!permitidos.Contains(extension))
    {
        return BadRequest("El archivo debe ser una imagen (JPG, JPEG o PNG).");
    }

    if (comprobanteFile.Length > 5 * 1024 * 1024)
    {
        return BadRequest("El archivo no debe exceder los 5MB.");
    }

    var identityUserId = _userManager.GetUserId(User);
    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);

    if (usuario == null)
    {
        return Unauthorized("Usuario no encontrado.");
    }

    // IMPORTANTE: Incluir ofertas en la consulta
    var carrito = await _context.Carritos
        .Include(c => c.CarritoAutopartes)
            .ThenInclude(cp => cp.Autoparte)
                .ThenInclude(a => a.Ofertas) // AGREGAR ESTA LÍNEA
        .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

    if (carrito == null || !carrito.CarritoAutopartes.Any())
    {
        return RedirectToAction("Index", "Carrito", new { mensaje = "Tu carrito está vacío" });
    }

    var metodoPagoId = 1;
    var metodoPago = await _context.MetodoPago.FindAsync(metodoPagoId);
    if (metodoPago == null)
    {
        return BadRequest("El método de pago especificado no es válido.");
    }

    // Calcular total con ofertas y descuentos para Yape
    decimal totalAutopartes = CalcularTotalAutopartes(carrito.CarritoAutopartes.ToList(), codigoDescuento);
    
    // Crear venta con total correcto
    var venta = new Venta
    {
        ven_fecha = DateOnly.FromDateTime(DateTime.Now),
        UsuarioId = usuario.usu_id,
        MetodoPagoId = metodoPagoId,
        Total = totalAutopartes, // Usar total calculado
        Estado = "Pendiente"
    };

    _context.Venta.Add(venta);
    await _context.SaveChangesAsync();

    // Marcar código de descuento como utilizado si existe
    await MarcarCodigoComoUtilizadoAsync(codigoDescuento);

    // Crear el comprobante
    var comprobante = new Comprobante
    {
        tipo_comprobante = tipoComprobante,
        VentaId = venta.ven_id,
        com_imagen = ""
    };
    _context.Comprobante.Add(comprobante);
    await _context.SaveChangesAsync();

    if (!string.IsNullOrEmpty(usuario.usu_correo))
    {
        await EnviarCorreoEstadoAsync(venta.ven_id, "Pendiente", usuario.usu_correo, $"{usuario.usu_nombre} {usuario.usu_apellido}", _emailSender);
    }

    // Crear Boleta o Factura según corresponda
    if (tipoComprobante?.ToLower() == "boleta")
    {
        var boleta = new Boleta
        {
            ComprobanteId = comprobante.com_id,
            num_identificacion = num_identificacion,
        };
        _context.Boleta.Add(boleta);
    }
    else if (tipoComprobante?.ToLower() == "factura")
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

    // Crear detalles de venta y actualizar stock
    foreach (var item in carrito.CarritoAutopartes)
    {
        var detalle = new DetalleVentas
        {
            VentaId = venta.ven_id,
            AutoParteId = item.AutoparteId,
            det_cantidad = item.car_cantidad
        };
        _context.DetalleVentas.Add(detalle);

        // Reducir stock
        var autoparte = await _context.Autopartes.FirstOrDefaultAsync(a => a.aut_id == item.AutoparteId);
        if (autoparte != null)
        {
            autoparte.aut_cantidad -= item.car_cantidad;
            _context.Autopartes.Update(autoparte);
        }

        // Crear salida
        var salida = new Salida
        {
            sal_fechasalida = DateOnly.FromDateTime(DateTime.Now),
            sal_cantidad = item.car_cantidad,
            ComprobanteId = comprobante.com_id,
            AutoparteId = item.AutoparteId
        };
        _context.Salida.Add(salida);
    }

    // Guardar imagen del comprobante
    string evidenciasDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "evidencias");
    if (!Directory.Exists(evidenciasDir))
    {
        Directory.CreateDirectory(evidenciasDir);
    }
    string nombreArchivo = $"evidencias_{comprobante.com_id}{extension}";
    string rutaCompleta = Path.Combine(evidenciasDir, nombreArchivo);

    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
    {
        await comprobanteFile.CopyToAsync(stream);
    }

    comprobante.com_evidencia = "/evidencias/" + nombreArchivo;
    _context.Comprobante.Update(comprobante);

    // Limpiar el carrito
    _context.CarritoAutopartes.RemoveRange(carrito.CarritoAutopartes);
    await _context.SaveChangesAsync();

    return RedirectToAction("PagoYapeExitoso", new { id = comprobante.com_id });
}



        // Nueva acción para mostrar la vista de éxito
        public async Task<IActionResult> PagoYapeExitoso(int id)
        {
            var comprobanteCompleto = await _context.Comprobante
                .Include(c => c.Boletas)
                .Include(c => c.Facturas)
                .FirstOrDefaultAsync(c => c.com_id == id);

            if (comprobanteCompleto == null)
                return RedirectToAction("Index", "Home");

            return View("PagoExitosoYape", comprobanteCompleto);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}