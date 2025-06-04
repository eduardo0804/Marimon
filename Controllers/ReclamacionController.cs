using Marimon.Data;
using Marimon.Models; // Asegúrate de tener el namespace correcto para el modelo Reclamacion
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Marimon.Enums;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Marimon.Controllers
{
    public class ReclamacionController : Controller
    {
        private readonly ILogger<ReclamacionController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ReclamacionController(ILogger<ReclamacionController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: Mostrar formulario
        public async Task<IActionResult> Index()
        {
            var identityUserId = _userManager.GetUserId(User);

            if (identityUserId == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.usu_id == identityUserId);

            if (usuario == null)
                return RedirectToAction("Login", "Account");

            var categorias = await _context.Categorias.ToListAsync();
            var productos = await _context.Autopartes.ToListAsync();
            var servicios = await _context.Servicio.ToListAsync();

            ViewBag.Usuario = usuario;
            ViewBag.Categorias = categorias;
            ViewBag.Productos = productos;
            ViewBag.Servicios = servicios;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerComprobantesUsuario(string usuarioId)
        {
            try
            {
                // Obtener todos los comprobantes del usuario
                var comprobantes = await _context.Comprobante
                    .Include(c => c.Venta)
                    .ThenInclude(v => v.Usuario)
                    .Include(c => c.Venta)
                    .ThenInclude(v => v.Detalles)
                    .ThenInclude(dv => dv.Autoparte)
                    .ThenInclude(a => a.Categoria)
                    .Where(c => c.Venta.Usuario.usu_id == usuarioId)
                    .Select(c => new
                    {
                        numero = c.com_id,
                        tipo = c.tipo_comprobante,
                        fecha = c.Venta.ven_fecha.ToString("dd/MM/yyyy"),
                        total = c.Venta.Total,
                        productos = c.Venta.Detalles.Select(dv => new
                        {
                            id = dv.Autoparte.aut_id,
                            nombre = dv.Autoparte.aut_nombre,
                            categoria = dv.Autoparte.Categoria.cat_nombre,
                            precio = dv.Autoparte.aut_precio,
                            cantidad = dv.det_cantidad,
                            montoTotal = dv.det_cantidad * dv.Autoparte.aut_precio
                        }).ToList()
                    })
                    .ToListAsync();

                var reservas = await _context.Reserva
                    .Include(r => r.Servicio)
                    .Include(r => r.Usuario)
                    .Where(r => r.Usuario.usu_id == usuarioId)
                    .Select(r => new
                    {
                        numero = r.res_id,
                        servicio = new
                        {
                            id = r.Servicio.ser_id,
                            nombre = r.Servicio.ser_nombre
                        },
                        fecha = r.res_fecha.ToString("dd/MM/yyyy"),
                        hora = r.res_hora.ToString(@"hh\:mm"),
                        estado = r.Estado
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    comprobantes = comprobantes,
                    reservas = reservas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener comprobantes del usuario");
                return Json(new { success = false, message = "Error al obtener comprobantes" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDatosComprobante(string numeroComprobante, string usuarioId)
        {
            try
            {
                if (!int.TryParse(numeroComprobante, out int numero))
                {
                    return Json(new { success = false, message = "Número de comprobante inválido" });
                }

                var comprobante = await _context.Comprobante
                    .Include(c => c.Venta)
                    .ThenInclude(v => v.Usuario)
                    .Include(c => c.Venta)
                    .ThenInclude(v => v.Detalles)
                    .ThenInclude(dv => dv.Autoparte)
                    .ThenInclude(a => a.Categoria)
                    .FirstOrDefaultAsync(c => c.com_id == numero && c.Venta.Usuario.usu_id == usuarioId);

                if (comprobante == null)
                {
                    return Json(new { success = false, message = "Comprobante no encontrado" });
                }

                var productos = comprobante.Venta.Detalles.Select(dv => new
                {
                    id = dv.Autoparte.aut_id,
                    nombre = dv.Autoparte.aut_nombre,
                    categoria = dv.Autoparte.Categoria.cat_nombre,
                    precio = dv.Autoparte.aut_precio,
                    cantidad = dv.det_cantidad,
                    montoTotal = dv.det_cantidad * dv.Autoparte.aut_precio
                }).ToList();

                return Json(new
                {
                    success = true,
                    productos = productos,
                    tieneProductos = productos.Any()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos del comprobante");
                return Json(new { success = false, message = "Error al obtener datos del comprobante" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDatosReserva(string numeroReserva, string usuarioId)
        {
            try
            {
                if (!int.TryParse(numeroReserva, out int numero))
                {
                    return Json(new { success = false, message = "Número de reserva inválido" });
                }

                var reserva = await _context.Reserva
                    .Include(r => r.Servicio)
                    .Include(r => r.Usuario)
                    .FirstOrDefaultAsync(r => r.res_id == numero && r.Usuario.usu_id == usuarioId);

                if (reserva == null)
                {
                    return Json(new { success = false, message = "Reserva no encontrada" });
                }

                var servicio = new
                {
                    id = reserva.Servicio.ser_id,
                    nombre = reserva.Servicio.ser_nombre,
                    reservaId = reserva.res_id,
                    fecha = reserva.res_fecha.ToString("dd/MM/yyyy"),
                    hora = reserva.res_hora.ToString(@"hh\:mm")
                };

                return Json(new
                {
                    success = true,
                    servicio = servicio,
                    tieneServicio = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de la reserva");
                return Json(new { success = false, message = "Error al obtener datos de la reserva" });
            }
        }
        // POST: Recibir datos del formulario y guardar la reclamación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearReclamacion(Reclamacion model)
        {
            try
            {
                var validacionComprobante = await ValidarComprobanteAsync(model.UsuarioId, model.NumeroReferencia);
                if (!validacionComprobante.EsValido)
                {
                    ModelState.AddModelError("NumeroReferencia", validacionComprobante.MensajeError);
                    await CargarDatosParaVista(model.UsuarioId);
                    return View("Index", model);
                }

                // NUEVA VALIDACIÓN: Verificar que el producto/servicio está en el comprobante
                var validacionProductoServicio = await ValidarProductoServicioEnComprobanteAsync(model);
                if (!validacionProductoServicio.EsValido)
                {
                    ModelState.AddModelError("EntidadId", validacionProductoServicio.MensajeError);
                    await CargarDatosParaVista(model.UsuarioId);
                    return View("Index", model);
                }

                _context.Reclamacion.Add(model);
                await _context.SaveChangesAsync();


                // Obtener datos del usuario
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.usu_id == model.UsuarioId);

                // Obtener nombre de la entidad según el tipo
                string nombreEntidad = "";
                string entidadLabel = "";

                if (model.TipoEntidad == TipoEntidad.Producto)
                {
                    var producto = await _context.Autopartes
                        .FirstOrDefaultAsync(p => p.aut_id == model.EntidadId);
                    nombreEntidad = producto?.aut_nombre ?? "Producto no encontrado";
                    entidadLabel = "Producto";
                }
                else if (model.TipoEntidad == TipoEntidad.Servicio)
                {
                    var servicio = await _context.Servicio
                        .FirstOrDefaultAsync(s => s.ser_id == model.EntidadId);
                    nombreEntidad = servicio?.ser_nombre ?? "Servicio no encontrado";
                    entidadLabel = "Servicio";
                }

                // Leer template del email
                var emailTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Emails", "emailRecla.html");
                string emailBody = await System.IO.File.ReadAllTextAsync(emailTemplatePath);

                // URL del logo
                var logoUrl = "https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png";

                // Determinar textos para el tipo de reclamación
                string tipoTexto = model.TipoReclamacion == TipoReclamacion.Reclamo ? "Reclamo" : "Queja";
                string tipoTextoLower = model.TipoReclamacion == TipoReclamacion.Reclamo ? "reclamo" : "queja";
                string tipoEntidadTexto = model.TipoEntidad == TipoEntidad.Producto ? "Producto" : "Servicio";
                string tipoEntidadTextoLower = model.TipoEntidad == TipoEntidad.Producto ? "producto" : "servicio";

                // Obtener fecha y hora actual
                var fechaActual = DateTime.Now;
                var fechaRegistro = fechaActual.ToString("dd/MM/yyyy");
                var horaRegistro = fechaActual.ToString("HH:mm");

                // Reemplazar todos los placeholders
                emailBody = emailBody.Replace("{{LogoUrl}}", logoUrl);
                emailBody = emailBody.Replace("{{NOMBRE_COMPLETO}}", $"{usuario?.usu_nombre} {usuario?.usu_apellido}");
                emailBody = emailBody.Replace("{{CORREO}}", usuario?.usu_correo ?? "No especificado");
                emailBody = emailBody.Replace("{{TIPO_RECLAMACION}}", tipoTexto);
                emailBody = emailBody.Replace("{{TIPO_RECLAMACION_LOWER}}", tipoTextoLower);
                emailBody = emailBody.Replace("{{TIPO_ENTIDAD}}", tipoEntidadTexto);
                emailBody = emailBody.Replace("{{TIPO_ENTIDAD_LOWER}}", tipoEntidadTextoLower);
                emailBody = emailBody.Replace("{{ENTIDAD_LABEL}}", entidadLabel);
                emailBody = emailBody.Replace("{{NOMBRE_ENTIDAD}}", nombreEntidad);
                emailBody = emailBody.Replace("{{NUMERO_REFERENCIA}}", model.NumeroReferencia ?? "No especificado");
                emailBody = emailBody.Replace("{{MONTO}}", model.Monto.ToString("F2"));
                emailBody = emailBody.Replace("{{ID_RECLAMACION}}", model.Id.ToString());
                emailBody = emailBody.Replace("{{DESCRIPCION}}", model.Descripcion ?? "Sin descripción");
                emailBody = emailBody.Replace("{{FECHA_REGISTRO}}", fechaRegistro);
                emailBody = emailBody.Replace("{{HORA_REGISTRO}}", horaRegistro);

                // Enviar correo
                await _emailSender.SendEmailAsync("marimonpruebas@gmail.com",
                    $"Nueva Reclamación #{model.Id} - {tipoTexto}",
                    emailBody);

                return RedirectToAction("Confirmacion");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear reclamación");
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                await CargarDatosParaVista(model.UsuarioId);
                return Content("Error al guardar: " + ex.Message + " | Detalle interno: " + innerMessage);
            }
        }

        // Método privado para cargar datos comunes para la vista
        private async Task CargarDatosParaVista(string userId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == userId);
            ViewBag.Usuario = usuario;
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            ViewBag.Productos = await _context.Autopartes.ToListAsync();
            ViewBag.Servicios = await _context.Servicio.ToListAsync();
        }

        private async Task<(bool EsValido, string MensajeError)> ValidarComprobanteAsync(string usuarioId, string numeroComprobante)
        {
            if (string.IsNullOrEmpty(numeroComprobante))
            {
                return (false, "El número de comprobante es requerido.");
            }

            var comprobante = await _context.Comprobante
                .Include(c => c.Venta)
                .ThenInclude(v => v.Usuario)
                .FirstOrDefaultAsync(c => c.com_id.ToString() == numeroComprobante &&
                                          c.Venta.Usuario.usu_id == usuarioId);

            if (comprobante != null)
            {
                return (true, "");
            }

            return (false, "El número de comprobante no existe o no pertenece a tu cuenta.");
        }

        private async Task<(bool EsValido, string MensajeError)> ValidarProductoServicioEnComprobanteAsync(Reclamacion model)
        {
            if (model.TipoEntidad == TipoEntidad.Producto)
            {
                var detalleVenta = await _context.DetalleVentas
                    .Include(dv => dv.Venta)
                    .ThenInclude(v => v.Usuario)
                    .Include(dv => dv.Autoparte)
                    .Where(dv => dv.AutoParteId == model.EntidadId &&
                                 dv.Venta.Usuario.usu_id == model.UsuarioId)
                    .FirstOrDefaultAsync();

                if (detalleVenta == null)
                {
                    return (false, "El producto seleccionado no está registrado en tus compras.");
                }

                var comprobanteProducto = await _context.Comprobante
                    .FirstOrDefaultAsync(c => c.VentaId == detalleVenta.VentaId &&
                                             c.com_id.ToString() == model.NumeroReferencia);

                if (comprobanteProducto == null)
                {
                    return (false, "El producto seleccionado no está registrado en este comprobante.");
                }

                var montoMaximo = detalleVenta.det_cantidad * detalleVenta.Autoparte.aut_precio;
                if (model.Monto > montoMaximo)
                {
                    return (false, $"El monto reclamado no puede exceder el monto pagado por este producto (S/. {montoMaximo:F2}).");
                }
            }
            else if (model.TipoEntidad == TipoEntidad.Servicio)
            {
                var reserva = await _context.Reserva
                    .Include(r => r.Usuario)
                    .Where(r => r.Usuario.usu_id == model.UsuarioId)
                    .ToListAsync(); 

                var reservaEncontrada = reserva
                    .FirstOrDefault(r => r.ser_id == model.EntidadId &&
                                        r.res_id.ToString() == model.NumeroReferencia);

                if (reservaEncontrada == null)
                {
                    return (false, "El servicio seleccionado no está registrado como una reserva tuya.");
                }

                if (model.Monto <= 0)
                {
                    return (false, "El monto debe ser mayor a cero.");
                }
                else if (model.Monto > 1000)
                {
                    return (false, "El monto no puede exceder S/. 1,000 para servicios.");
                }
            }

            return (true, "");
        }

        public IActionResult Confirmacion()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}