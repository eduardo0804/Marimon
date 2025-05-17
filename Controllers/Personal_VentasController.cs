using System.Transactions;
using Google.Apis.Auth.OAuth2;
using Marimon.Data;
using Marimon.Models;
using Marimon.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace Marimon.Controllers
{
    public class Personal_VentasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<Personal_VentasController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailSenderWithAttachments _emailSender;
        private readonly IConverter _converter;
        private readonly IOptions<EmailSettings> _emailSettings;
        public Personal_VentasController(
            ApplicationDbContext context,
            ILogger<Personal_VentasController> logger,
            IWebHostEnvironment hostingEnvironment,
            IEmailSenderWithAttachments emailSender,
            IOptions<EmailSettings> emailSettings,
            IConverter converter) // Añade este parámetro
        {
            _context = context;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _emailSender = emailSender;
            _emailSettings = emailSettings; // Añade esta asignación
            _converter = converter;
        }

        // GET: Personal_VentasController
        public ActionResult Index()
        {
            return View();
        }

        // GET: Admin/Entradas - Página de entrada de productos
        public ActionResult Entradas()
        {
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");

            // Cargar la lista de entradas para mostrar en la tabla
            var listaEntradas = _context.Entradas
                .Include(e => e.Autoparte)
                .OrderByDescending(e => e.ent_id)
                .Take(10)  // Limitar a las últimas 20 entradas
                .ToList();

            ViewBag.ListaEntradas = listaEntradas;

            return View("~/Views/Flujos/Entradas.cshtml");
        }

        // AJAX - Obtener productos por categoría
        [HttpGet]
        public IActionResult ObtenerProductosPorCategoria(int categoriaId)
        {
            try
            {
                // Usar una clase auxiliar en lugar de un tipo anónimo
                var productos = _context.Autopartes
                    .Where(p => p.CategoriaId == categoriaId)
                    .Select(p => new ProductoDTO
                    {
                        aut_id = p.aut_id,
                        aut_nombre = p.aut_nombre ?? string.Empty
                    })
                    .ToList();

                return Json(productos);
            }
            catch (Exception ex)
            {
                var errorResponse = new Dictionary<string, string> { { "error", "Error al obtener productos" } };
                return Json(errorResponse);
            }
        }

        // Clase DTO para evitar tipos anónimos
        public class ProductoDTO
        {
            public int aut_id { get; set; }
            public string aut_nombre { get; set; } = string.Empty;
        }

        // POST: Admin/RegistrarEntrada - Procesar entrada de productos
        [HttpPost]
        public IActionResult RegistrarEntrada(int AutoparteId, int ent_cantidad, string ent_proveedor = "")
        {

            try
            {
                // Validar entradas
                if (AutoparteId <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un producto válido.";
                    return RedirectToAction("Entradas");
                }

                if (ent_cantidad <= 0)
                {
                    TempData["Error"] = "La cantidad debe ser mayor a 0.";
                    return RedirectToAction("Entradas");
                }

                // Buscar la autoparte correspondiente
                var autoparte = _context.Autopartes.Find(AutoparteId);
                if (autoparte == null)
                {
                    TempData["Error"] = "No se encontró el producto seleccionado.";
                    return RedirectToAction("Entradas");
                }

                // Crear el registro de entrada
                var entrada = new Entradas
                {
                    AutoparteId = AutoparteId,
                    ent_cantidad = ent_cantidad,
                    ent_proveedor = ent_proveedor,
                    ent_fechaent = DateOnly.FromDateTime(DateTime.Now),
                };

                autoparte.aut_cantidad += ent_cantidad;

                _context.Entradas.Add(entrada);
                _context.Autopartes.Update(autoparte);
                _context.SaveChanges();

                TempData["Mensaje"] = $"Se han registrado {ent_cantidad} unidades de {autoparte.aut_nombre} correctamente.";
                return RedirectToAction("Entradas");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar entrada: {ex.Message}");
                TempData["Error"] = "Error al registrar la entrada: " + ex.Message;
                return RedirectToAction("Entradas");
            }
        }

        // Fix for CS8602: Desreferencia de una referencia posiblemente NULL.
        public ActionResult Salidas()
        {
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
            // Agrupar las salidas por VentaId
            var salidasAgrupadas = _context.Salida
                .Include(s => s.Autoparte)
                .Include(s => s.Comprobante)
                    .ThenInclude(c => c.Venta)
                .Where(s => s.Comprobante != null && s.Comprobante.Venta != null)
                .OrderByDescending(s => s.sal_id)
                .GroupBy(s => s.Comprobante!.Venta!.ven_id)
                .Select(g => new
                {
                    VentaId = g.Key,
                    Estado = g.FirstOrDefault()!.Comprobante!.Venta!.Estado, // Incluir el estado de la venta
                    Salidas = g.ToList(),

                })
                .ToList();

            // Obtener las salidas que no tienen VentaId
            var salidasSinVenta = _context.Salida
                .Include(s => s.Autoparte)
                .Where(s => s.Comprobante == null || s.Comprobante.Venta == null)
                .OrderByDescending(s => s.sal_id)
                .Select(s => new
                {
                    SalidaId = s.sal_id,
                    Producto = s.Autoparte.aut_nombre,
                    Cantidad = s.sal_cantidad,
                    FechaSalida = s.sal_fechasalida,
                    VentaId = 0,
                    Estado = "Completado"
                })
                .ToList();

            ViewBag.SalidasAgrupadas = salidasAgrupadas;
            ViewBag.SalidasSinVenta = salidasSinVenta;
            return View("~/Views/Flujos/Salida.cshtml");
        }




        // POST: Admin/RegistrarSalida - Procesar salida de productos
        [HttpPost]
        public async Task<IActionResult> RegistrarSalida(int AutoparteId, int sal_cantidad)
        {
            try
            {
                // Validar entradas
                if (AutoparteId <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un producto válido.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }

                if (sal_cantidad <= 0)
                {
                    TempData["Error"] = "La cantidad debe ser mayor a 0.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }

                // Buscar la autoparte correspondiente
                var autoparte = await _context.Autopartes.FindAsync(AutoparteId);
                if (autoparte == null)
                {
                    TempData["Error"] = "No se encontró el producto seleccionado.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }
                else if (autoparte.aut_cantidad < sal_cantidad)
                {
                    TempData["Error"] = "No hay suficiente inventario para realizar la salida.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }

                // Validar stock disponible
                if (autoparte.aut_cantidad < sal_cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente. Solo hay {autoparte.aut_cantidad} unidades disponibles.";
                    return RedirectToAction("Salidas");
                }

                // Paso 6: Registrar la salida
                var salida = new Salida
                {
                    AutoparteId = AutoparteId,
                    sal_cantidad = sal_cantidad,
                    sal_fechasalida = DateOnly.FromDateTime(DateTime.Now),
                    // ComprobanteId = comprobante.com_id // Relacionar con el comprobante ya guardado
                };
                _context.Salida.Add(salida);

                // ADDED: Update inventory quantity
                autoparte.aut_cantidad -= sal_cantidad;
                _context.Autopartes.Update(autoparte);

                await _context.SaveChangesAsync(); // Guardar todo
                TempData["ShowModal"] = true;
                TempData["ProductoNombre"] = autoparte.aut_nombre;
                TempData["Cantidad"] = sal_cantidad;

                TempData["Mensaje"] = $"Se han registrado la salida de {sal_cantidad} unidades de {autoparte.aut_nombre} correctamente.";
                return RedirectToAction("Salidas");  // FIXED: Changed to redirect to Salidas instead of returning Ok
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar salida: {ex.Message}");
                TempData["Error"] = "Error al registrar la salida: " + ex.Message;  // FIXED: Changed "entrada" to "salida"
                return RedirectToAction("Salidas");
            }
        }

        // Fix for CS0103 and CS1525: Define the missing method 'EnviarNotiEmail' and provide the required parameters.
        private void EnviarNotiEmail(string estado, int ventaId)
        {
            // Implement the logic to send a notification email based on the state and ventaId.
            _logger.LogInformation($"Notificación enviada para la venta #{ventaId} con estado: {estado}");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoAvanzado(int id, string estado, [FromServices] IEmailSenderWithAttachments emailSender)
        {
            Venta? venta = null;
            string estadoAnterior = "";
            string emailBody = "";
            string subject = "";
            try
            {
                // Validar el estado
                if (estado != "Completado" && estado != "Pendiente" && estado != "Cancelado")
                {
                    TempData["Error"] = "Estado no válido.";
                    return RedirectToAction("Salidas");
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    // Buscar la venta por ID incluyendo el Usuario relacionado
                    venta = await _context.Venta
                        .Include(v => v.Usuario)
                        .FirstOrDefaultAsync(v => v.ven_id == id);

                    if (venta == null)
                    {
                        TempData["Error"] = "Venta no encontrada.";
                        return RedirectToAction("Salidas");
                    }

                    estadoAnterior = venta.Estado;
                    if (estadoAnterior == estado)
                    {
                        TempData["Mensaje"] = $"La venta #{id} ya se encuentra en estado {estado}.";
                        return RedirectToAction("Salidas");
                    }

                    // Actualizar el estado
                    venta.Estado = estado;
                    await _context.SaveChangesAsync();

                    bool esStripe = !string.IsNullOrEmpty(venta.StripeSessionId);
                    bool esCancelacion = estado == "Cancelado";

                    if (esStripe && esCancelacion)
                    {
                        // Usar plantilla de cancelación específica para Stripe
                        var cancelacionTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Emails", "Cancelacion.html");
                        emailBody = await System.IO.File.ReadAllTextAsync(cancelacionTemplatePath);
                        subject = $"Cancelación de tu pedido #{id} - Marimon Autopartes";

                        // Reemplazar los valores dinámicos en la plantilla de cancelación
                        emailBody = emailBody.Replace("{nombreCompleto}", venta.Usuario?.usu_nombre + " " + venta.Usuario?.usu_apellido)
                                        .Replace("{ventaID}", id.ToString())
                                        .Replace("{montoTotal}", venta.Total.ToString("0.00"))
                                        .Replace("{{CallbackUrl}}", "http://localhost:5031/Catalogo");

                    }
                    else
                    {

                        // Cargar el contenido del archivo HTML
                        var emailTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Emails", "EstadosPedido.html");
                        emailBody = await System.IO.File.ReadAllTextAsync(emailTemplatePath);

                        // Determinar las clases CSS según el estado
                        string stylePendiente = "opacity:0.4; color:#F39C12; font-weight:bold; font-size:14px; padding:0 15px;";
                        string styleCompletado = "opacity:0.4; color:#27ae60; font-weight:bold; font-size:14px; padding:0 15px;";
                        string styleCancelado = "opacity:0.4; color:#E74C3C; font-weight:bold; font-size:14px; padding:0 15px;";
                        string claseEstadoTexto = "";
                        string colCancelado = "";
                        subject = $"Estado de tu pedido #{id} actualizado a {estado}";
                        string mensajeEstado = "";
                        string mensajeCambio = $"El estado de tu pedido <strong>#{id}</strong> ha cambiado a:";

                        if (estado == "Pendiente")
                        {
                            stylePendiente = "opacity:1; color:#F39C12; font-weight:bold; font-size:14px; padding:0 15px;";
                            claseEstadoTexto = "color: #F39C12;";
                            mensajeEstado = "Tu pedido está pendiente de confirmación. Estamos revisando tu pago y te notificaremos cuando se confirme.";
                        }
                        else if (estado == "Completado")
                        {
                            styleCompletado = "opacity:1; color:#27ae60; font-weight:bold; font-size:14px; padding:0 15px;";
                            claseEstadoTexto = "color: #27ae60;";
                            mensajeEstado = "¡Tu pedido ha sido completado exitosamente! Puedes acercarte a nuestro local para recogerlo. <br><b>Adjuntamos tu boleta electrónica en este correo para tu comodidad y respaldo.</b> ¡Gracias por confiar en Marimon!";
                            // Buscar el comprobante relacionado
                            var comprobante = await _context.Comprobante
                                .Include(c => c.Boletas)
                                .FirstOrDefaultAsync(c => c.VentaId == venta.ven_id);

                            if (comprobante != null)
                            {
                                // Generar HTML y PDF SOLO si com_imagen está vacío o el archivo no existe
                                if (string.IsNullOrEmpty(comprobante.com_imagen) ||
                                    !System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", comprobante.com_imagen?.TrimStart('/'))))
                                {
                                    var htmlContent = GenerateComprobanteHtml(comprobante);
                                    var pdfBytes = ConvertHtmlToPdf(htmlContent);
                                    var rutaPdf = SavePdfToFile(pdfBytes, comprobante.com_id, comprobante.tipo_comprobante);

                                    comprobante.com_imagen = rutaPdf;
                                    _context.Update(comprobante);
                                    await _context.SaveChangesAsync();
                                }
                            }
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

                        // Reemplazar los valores dinámicos
                        emailBody = emailBody.Replace("{{UserName}}", venta.Usuario?.usu_nombre ?? "Cliente")
                                            .Replace("{{PedidoId}}", id.ToString())
                                            .Replace("{{Estado}}", estado)
                                            .Replace("{{LogoUrl}}", "https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png")
                                            .Replace("{{CallbackUrl}}", "http://localhost:5031/Identity/Account/Manage/Pedidos")
                                            .Replace("{{StylePendiente}}", stylePendiente)
                                            .Replace("{{StyleCompletado}}", styleCompletado)
                                            .Replace("{{StyleCancelado}}", styleCancelado)
                                            .Replace("{{ColCancelado}}", colCancelado)
                                            .Replace("{{MensajeEstado}}", mensajeEstado)
                                            .Replace("{{MensajeCambio}}", mensajeCambio)
                                            .Replace("{{ClaseEstadoTexto}}", claseEstadoTexto);
                    }
                    // Confirmar la transacción
                    scope.Complete();
                }

                // Enviar correo fuera de la transacción para evitar bloqueos y reenviar PDF si ya existe
                if (venta.Usuario != null && !string.IsNullOrEmpty(venta.Usuario.usu_correo))
                {
                    try
                    {
                        if (estado == "Completado")
                        {
                            var comprobante = await _context.Comprobante
                                .Include(c => c.Boletas)
                                .FirstOrDefaultAsync(c => c.VentaId == venta.ven_id);

                            if (comprobante != null && !string.IsNullOrEmpty(comprobante.com_imagen))
                            {
                                var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", comprobante.com_imagen.TrimStart('/'));
                                if (System.IO.File.Exists(pdfPath))
                                {
                                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
                                    var fileName = Path.GetFileName(pdfPath);

                                    await emailSender.SendEmailWithAttachmentAsync(
                                        venta.Usuario.usu_correo,
                                        subject,
                                        emailBody,
                                        pdfBytes,
                                        fileName,
                                        "application/pdf"
                                    );
                                }
                                else
                                {
                                    await emailSender.SendEmailAsync(venta.Usuario.usu_correo, subject, emailBody);
                                }
                            }
                            else
                            {
                                await emailSender.SendEmailAsync(venta.Usuario.usu_correo, subject, emailBody);
                            }
                        }
                        else
                        {
                            await emailSender.SendEmailAsync(venta.Usuario.usu_correo, subject, emailBody);
                        }

                        TempData["Mensaje"] = $"El estado de la venta #{id} ha sido actualizado de {estadoAnterior} a {estado}. Se ha enviado notificación por correo al usuario.";
                        _logger.LogInformation($"Correo enviado al usuario {venta.Usuario.usu_correo} con el estado '{estado}'.");
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"El estado de la venta #{id} se actualizó, pero no se pudo enviar el correo: {ex.Message}";
                        _logger.LogError($"Error al enviar el correo: {ex.Message}");
                    }
                }
                else
                {
                    TempData["Mensaje"] = $"El estado de la venta #{id} ha sido actualizado de {estadoAnterior} a {estado}. No se envió notificación porque el usuario no tiene correo registrado.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar el estado: {ex.Message}";
            }

            return RedirectToAction("Salidas");
        }

        private void EnviarNotiEmail()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDatosVenta(int ventaId)
        {
            try
            {
                var venta = await _context.Venta
                    .Include(v => v.Usuario)
                    .Include(v => v.MetodoPago)
                    .FirstOrDefaultAsync(v => v.ven_id == ventaId);

                if (venta == null)
                {
                    return Json(new { error = "Venta no encontrada" });
                }

                var comprobante = await _context.Comprobante
                    .Include(c => c.Boletas)
                    .Include(c => c.Facturas)
                    .FirstOrDefaultAsync(c => c.VentaId == ventaId);

                string cliente = venta.Usuario?.usu_nombre + " " + venta.Usuario?.usu_apellido;
                string documento = "";
                string evidenciaUrl = "";
                bool esStripe = false;

                esStripe = !string.IsNullOrEmpty(venta.StripeSessionId);

                if (comprobante != null)
                {
                    if (comprobante.Facturas != null && comprobante.Facturas.Any())
                    {
                        var factura = comprobante.Facturas.First();
                        cliente = factura.fac_razonsocial;
                        documento = "RUC: " + factura.fac_ruc;
                    }
                    else if (comprobante.Boletas != null && comprobante.Boletas.Any())
                    {
                        var boleta = comprobante.Boletas.First();
                        documento = "DNI: " + boleta.num_identificacion;
                    }

                    // Obtener la URL de la evidencia solo para pagos que no son por Stripe
                    if (!esStripe && !string.IsNullOrEmpty(comprobante.com_evidencia))
                    {
                        evidenciaUrl = comprobante.com_evidencia;
                        if (!evidenciaUrl.StartsWith("http") && !evidenciaUrl.StartsWith("/"))
                        {
                            evidenciaUrl = "/" + evidenciaUrl;
                        }
                    }
                }

                // Obtener el nombre del método de pago
                string metodoPago = venta.MetodoPago?.pag_metodo ?? "No especificado";
                if (esStripe)
                {
                    metodoPago += " (Stripe)";
                }

                return Json(new
                {
                    cliente = cliente,
                    documento = documento,
                    total = venta.Total.ToString("0.00"),
                    evidenciaUrl = evidenciaUrl,
                    esStripe = esStripe,
                    metodoPago = metodoPago
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de la venta {VentaId}", ventaId);
                return Json(new { error = "Error al procesar la solicitud: " + ex.Message });
            }
        }




        //AUTOPARTE - CRUD

        // GET: Admin/ListaAutopartes
        public IActionResult ListaAutopartes(string orden = null, List<int> categorias = null)
        {
            var autopartes = _context.Autopartes.Include(a => a.Categoria).AsQueryable();

            if (categorias != null && categorias.Any())
            {
                autopartes = autopartes.Where(a => categorias.Contains(a.CategoriaId));
            }

            var ventasOnline = _context.DetalleVentas
                .GroupBy(d => d.AutoParteId)
                .Select(g => new { AutoparteId = g.Key, Cantidad = g.Sum(d => d.det_cantidad) });

            var ventasPresenciales = _context.Salida
                .GroupBy(s => s.AutoparteId)
                .Select(g => new { AutoparteId = g.Key, Cantidad = g.Sum(s => s.sal_cantidad) });

            var ventasTotales = ventasOnline
                .Concat(ventasPresenciales)
                .GroupBy(v => v.AutoparteId)
                .Select(g => new { AutoparteId = g.Key, Total = g.Sum(x => x.Cantidad) })
                .ToList();

            if (orden == "asc")
            {
                autopartes = autopartes.OrderBy(a => a.aut_precio);
            }
            else if (orden == "desc")
            {
                autopartes = autopartes.OrderByDescending(a => a.aut_precio);
            }
            else if (orden == "mas_vendidas")
            {
                autopartes = autopartes
                    .AsEnumerable()
                    .OrderByDescending(a =>
                        ventasTotales.FirstOrDefault(v => v.AutoparteId == a.aut_id)?.Total ?? 0)
                    .AsQueryable();
            }
            else
            {
                autopartes = autopartes.OrderBy(a => a.aut_id);
            }

            var lista = autopartes.ToList();

            // Asignar la cantidad vendida
            foreach (var autoparte in lista)
            {
                autoparte.CantidadVendida = ventasTotales
                    .FirstOrDefault(v => v.AutoparteId == autoparte.aut_id)?.Total ?? 0;
            }

            ViewBag.Categorias = _context.Categorias.ToList();
            return View(lista);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Registrar Autoparte";
            ViewBag.ButtonText = "Registrar";
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
            return PartialView("Create"); // Asegúrate de que sea una vista parcial
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Autoparte autoparte, IFormFile imagen)
        {
            try
            {
                var bucket = "marimonapp.appspot.com";
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);

                // Ruta variable de entorno para las credenciales de Firebase
                var credJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
                if (string.IsNullOrEmpty(credJson))
                {
                    throw new Exception("No se encontró la variable de entorno FIREBASE_CREDENTIALS.");
                }

                var credential = GoogleCredential.FromJson(credJson)
                    .CreateScoped("https://www.googleapis.com/auth/devstorage.full_control");

                var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    using (var stream = imagen.OpenReadStream())
                    {
                        var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name=autopartes/{uniqueFileName}";
                        var content = new StreamContent(stream);
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                        var response = await httpClient.PostAsync(url, content);
                        response.EnsureSuccessStatusCode();

                        // URL pública de la imagen
                        var publicUrl = $"https://storage.googleapis.com/{bucket}/autopartes/{uniqueFileName}";
                        autoparte.aut_imagen = publicUrl;
                    }
                }
                _context.Autopartes.Add(autoparte);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "La autoparte se registró correctamente.";

                return Content("<script>window.parent.location.href = '/Personal_Ventas/ListaAutopartes';</script>", "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar la autoparte");
                ModelState.AddModelError("", $"Error al registrar la autoparte: {ex.Message}");
                ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
                return View(autoparte);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var autoparte = await _context.Autopartes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.aut_id == id);

            if (autoparte == null)
            {
                return NotFound();
            }

            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre", autoparte.CategoriaId);
            return PartialView("_EditarAutoparte", autoparte);
        }
        [HttpPost]
        public async Task<IActionResult> Editar(Autoparte autoparte, IFormFile imagen)
        {
            // Recuperar la autoparte existente desde la base de datos
            var autoparteExistente = await _context.Autopartes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.aut_id == autoparte.aut_id);

            if (autoparteExistente == null)
            {
                return NotFound();
            }

            if (imagen != null && imagen.Length > 0)
            {
                // Subir la nueva imagen a Firebase Storage
                var bucket = "marimonapp.appspot.com";
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);

                var credJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
                if (string.IsNullOrEmpty(credJson))
                {
                    throw new Exception("No se encontró la variable de entorno FIREBASE_CREDENTIALS.");
                }

                var credential = GoogleCredential.FromJson(credJson)
                    .CreateScoped("https://www.googleapis.com/auth/devstorage.full_control");

                var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    using (var stream = imagen.OpenReadStream())
                    {
                        var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name=autopartes/{uniqueFileName}";
                        var content = new StreamContent(stream);
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                        var response = await httpClient.PostAsync(url, content);
                        response.EnsureSuccessStatusCode();

                        var publicUrl = $"https://storage.googleapis.com/{bucket}/autopartes/{uniqueFileName}";
                        autoparte.aut_imagen = publicUrl;
                    }

                    // Eliminar la imagen anterior de Firebase Storage si existe
                    if (!string.IsNullOrEmpty(autoparteExistente.aut_imagen))
                    {
                        var oldImageName = autoparteExistente.aut_imagen.Split('/').Last();
                        var deleteUrl = $"https://storage.googleapis.com/storage/v1/b/{bucket}/o/autopartes%2F{oldImageName}";

                        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                        deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                        var deleteResponse = await httpClient.SendAsync(deleteRequest);
                        if (!deleteResponse.IsSuccessStatusCode)
                        {
                            _logger.LogWarning($"No se pudo eliminar la imagen anterior: {autoparteExistente.aut_imagen}");
                        }
                    }
                }
            }
            else
            {
                // Mantener la imagen anterior si no se sube una nueva
                autoparte.aut_imagen = autoparteExistente.aut_imagen;
            }

            // Actualizar los datos de la autoparte
            _context.Autopartes.Update(autoparte);
            await _context.SaveChangesAsync();

            TempData["EditMessage"] = "La autoparte se actualizó correctamente.";
            return RedirectToAction("ListaAutopartes", "Personal_Ventas");
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var autoparte = await _context.Autopartes.FindAsync(id);

            if (autoparte == null)
            {
                return NotFound();
            }

            // Eliminar la imagen del servidor si existe
            if (!string.IsNullOrEmpty(autoparte.aut_imagen))
            {
                var rutaImagen = Path.Combine(_hostingEnvironment.WebRootPath, autoparte.aut_imagen.TrimStart('/'));

                if (System.IO.File.Exists(rutaImagen))
                {
                    System.IO.File.Delete(rutaImagen);
                }
            }

            // Eliminar la autoparte de la base de datos
            _context.Autopartes.Remove(autoparte);
            await _context.SaveChangesAsync();
            TempData["DeleteMessage"] = "La autoparte se eliminó correctamente.";
            return RedirectToAction("ListaAutopartes", "Personal_Ventas");
        }

        private string GenerateComprobanteHtml(Comprobante comprobante)
        {
            var comprobanteCompleto = _context.Comprobante
                .Include(c => c.Venta)
                .Include(c => c.Boletas)
                .Include(c => c.Facturas)
                .FirstOrDefault(c => c.com_id == comprobante.com_id);

            if (comprobanteCompleto == null || comprobanteCompleto.Venta == null)
                return "<p>Error: Comprobante no válido.</p>";

            var venta = comprobanteCompleto.Venta;
            var detallesVenta = _context.DetalleVentas
                .Where(dv => dv.VentaId == venta.ven_id)
                .Include(dv => dv.Autoparte)
                .ToList();
            var usuario = _context.Usuarios.FirstOrDefault(u => u.usu_id == venta.UsuarioId);

            string numeroComprobante = $"{(comprobanteCompleto.tipo_comprobante.ToLower() == "factura" ? "F" : "B")}001-{comprobanteCompleto.com_id.ToString("0000000")}";
            string fechaEmision = DateTime.Now.ToString("dd/MM/yyyy");
            decimal subtotal = Math.Round(venta.Total / 1.18m, 2);
            decimal igv = Math.Round(venta.Total - subtotal, 2);

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

            // Detalles de productos
            foreach (var detalle in detallesVenta)
            {
                var autoparte = detalle.Autoparte;
                decimal precioUnitario = Math.Round(autoparte.aut_precio / 1.18m, 2);
                decimal subTotalItem = Math.Round(autoparte.aut_precio * detalle.det_cantidad, 2);
                decimal totalItem = Math.Round(autoparte.aut_precio * detalle.det_cantidad, 2);

                htmlContent += $@"
                <tr>
                    <td>{detalle.det_cantidad}</td>
                    <td>{autoparte.aut_id}</td>
                    <td class='description'>{autoparte.aut_nombre}</td>
                    <td class='right'>{precioUnitario:N2}</td>
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
            var doc = new DinkToPdf.HtmlToPdfDocument()
            {
                GlobalSettings = {
            ColorMode = DinkToPdf.ColorMode.Color,
            Orientation = DinkToPdf.Orientation.Portrait,
            PaperSize = DinkToPdf.PaperKind.A4
        },
                Objects = {
            new DinkToPdf.ObjectSettings() {
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" }
            }
        }
            };
            // Debes tener _converter inyectado en el constructor
            return _converter.Convert(doc);
        }

        private string SavePdfToFile(byte[] pdfBytes, int comprobanteId, string tipoComprobante)
        {
            string directorio = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "comprobantes");
            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }
            string nombreArchivo = $"{tipoComprobante.ToLower()}_{comprobanteId}.pdf";
            string rutaArchivo = Path.Combine(directorio, nombreArchivo);
            System.IO.File.WriteAllBytes(rutaArchivo, pdfBytes);
            return $"/comprobantes/{nombreArchivo}";
        }
    }
}