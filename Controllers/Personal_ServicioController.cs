using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Marimon.Data;
using Marimon.Enums;
using Marimon.Models;
using Marimon.Services;
using Marimon.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class Personal_ServicioController : Controller
    {
        private readonly ILogger<Personal_ServicioController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PdfGeneratorService _pdfService;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;


        public Personal_ServicioController(ILogger<Personal_ServicioController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager, PdfGeneratorService pdfService,
        ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _pdfService = pdfService;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult LServicio()
        {
            var personalServicio = _context.Servicio.ToList();
            return View(personalServicio);
        }
        public IActionResult CServicio()
        {
            var servicios = _context.Servicio.ToList();
            return View("CServicio", servicios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CServicio(Servicio servicio, IFormFile imagen)
        {
            try
            {
                if (imagen != null && imagen.Length > 0)
                {
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
                            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name=servicios/{uniqueFileName}";
                            var content = new StreamContent(stream);
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                            var response = await httpClient.PostAsync(url, content);
                            response.EnsureSuccessStatusCode();

                            var publicUrl = $"https://storage.googleapis.com/{bucket}/servicios/{uniqueFileName}";
                            servicio.ser_imagen = publicUrl;
                        }
                    }
                }

                _context.Servicio.Add(servicio);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "El servicio se registró correctamente.";

                return Content("<script>window.parent.location.href = '/Personal_Servicio/LServicio';</script>", "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el servicio");
                ModelState.AddModelError("", $"Error al registrar el servicio: {ex.Message}");
                return View("CrearForm", servicio);
            }
        }

        public IActionResult ConsultarServicios(string nombreServicio, string fechaDesde, string fechaHasta, string estado)
        {
            // Parse fechas y servicios como antes
            var serviciosSeleccionados = string.IsNullOrEmpty(nombreServicio)
                ? new List<string>()
                : nombreServicio.Split(',').ToList();

            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;

            if (DateTime.TryParse(fechaDesde, out var desde))
                fechaInicio = DateTime.SpecifyKind(desde, DateTimeKind.Utc);

            if (DateTime.TryParse(fechaHasta, out var hasta))
                fechaFin = DateTime.SpecifyKind(hasta, DateTimeKind.Utc);

            // OBTENER TODAS LAS RESERVAS PRIMERO (sin filtro de estado)
            var todasLasReservas = _context.Reserva
                .Include(r => r.Servicio)
                .Include(r => r.Usuario)
                .Where(r =>
                    (serviciosSeleccionados.Count == 0 || serviciosSeleccionados.Contains(r.Servicio.ser_nombre)) &&
                    (!fechaInicio.HasValue || r.res_fecha >= fechaInicio.Value) &&
                    (!fechaFin.HasValue || r.res_fecha <= fechaFin.Value)
                // NO filtrar por estado aquí
                )
                .OrderBy(r => r.res_fecha)
                .ThenBy(r => r.res_hora)
                .ToList();

            // AHORA aplicar el filtro de estado solo para mostrar en la tabla
            var reservasFiltradas = todasLasReservas;
            if (!string.IsNullOrEmpty(estado))
            {
                reservasFiltradas = todasLasReservas
                    .Where(r => r.Estado.ToString() == estado)
                    .ToList();
            }

            // IMPORTANTE: Siempre mostrar TODOS los estados posibles
            var todosLosEstados = new List<string>
            {
                "Pendiente",
                "Confirmada",
                "Completada",
                "Cancelada"
            };

            ViewBag.EstadoSeleccionado = estado;
            // Usar todos los estados, NO los estados de las reservas filtradas
            ViewBag.Estados = todosLosEstados;

            // Para calcular los contadores correctos, usar todas las reservas (sin filtro de estado)
            ViewBag.ContadoresEstados = new Dictionary<string, int>
            {
                ["Todos"] = todasLasReservas.Count(),
                ["Pendiente"] = todasLasReservas.Count(r => r.Estado == EstadoReserva.Pendiente),
                ["Confirmada"] = todasLasReservas.Count(r => r.Estado == EstadoReserva.Confirmada),
                ["Completada"] = todasLasReservas.Count(r => r.Estado == EstadoReserva.Completada),
                ["Cancelada"] = todasLasReservas.Count(r => r.Estado == EstadoReserva.Cancelada)
            };

            var model = new ServicioReservaViewModel
            {
                Servicios = _context.Servicio.ToList(),
                Reservas = reservasFiltradas, // Solo las reservas filtradas para mostrar en la tabla
                ServiciosSeleccionados = serviciosSeleccionados
            };

            return View(model);
        }

        public IActionResult MServicio()
        {
            var personalServicio = _context.Servicio.ToList();
            return View(personalServicio);
        }

        public IActionResult Editar(int id)
        {
            var servicio = _context.Servicio.FirstOrDefault(s => s.ser_id == id);
            if (servicio == null)
            {
                return NotFound();
            }
            return PartialView("MFServicio", servicio);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(Servicio servicio, IFormFile imagen)
        {
            try
            {
                var servicioExistente = _context.Servicio.FirstOrDefault(s => s.ser_id == servicio.ser_id);
                if (servicioExistente == null)
                {
                    return NotFound();
                }

                servicioExistente.ser_nombre = servicio.ser_nombre;
                servicioExistente.ser_descripcion = servicio.ser_descripcion;

                if (imagen != null && imagen.Length > 0)
                {
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
                            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name=servicios/{uniqueFileName}";
                            var content = new StreamContent(stream);
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                            var response = await httpClient.PostAsync(url, content);
                            response.EnsureSuccessStatusCode();

                            var publicUrl = $"https://storage.googleapis.com/{bucket}/servicios/{uniqueFileName}";
                            servicioExistente.ser_imagen = publicUrl;
                        }
                    }
                }

                _context.Servicio.Update(servicioExistente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "El servicio se actualizó correctamente.";

                return RedirectToAction("LServicio");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el servicio");
                ModelState.AddModelError("", $"Error al actualizar el servicio: {ex.Message}");
                return View("MFServicio", servicio);
            }
        }
        public IActionResult EServicio()
        {
            var personalServicio = _context.Servicio.ToList();
            return View(personalServicio);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            try
            {
                var servicio = _context.Servicio.FirstOrDefault(s => s.ser_id == id);
                if (servicio == null)
                {
                    TempData["ErrorMessage"] = "El servicio no existe.";
                    return RedirectToAction("EServicio");
                }

                _context.Servicio.Remove(servicio);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "El servicio se eliminó correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el servicio");
                TempData["ErrorMessage"] = "Ocurrió un error al intentar eliminar el servicio.";
            }

            return RedirectToAction("LServicio");
        }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public IActionResult CambiarEstadoReserva(int reservaId, string nuevoEstado)
        // {
        //     var reserva = _context.Reserva.FirstOrDefault(r => r.res_id == reservaId);
        //     if (reserva == null) return NotFound();

        //     if (Enum.TryParse<EstadoReserva>(nuevoEstado, out var estado))
        //     {
        //         reserva.Estado = estado;
        //         _context.SaveChanges();
        //         return RedirectToAction("ConsultarServicios"); // o la acción donde muestras la tabla
        //     }

        //     return BadRequest("Estado inválido");
        // }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoReserva(int reservaId, string nuevoEstado, [FromServices] IEmailSenderWithAttachments emailSender)
        {
            try
            {
                if (!Enum.TryParse<EstadoReserva>(nuevoEstado, out var estado))
                {
                    TempData["Error"] = "Estado no válido.";
                    return RedirectToAction("ConsultarServicios");
                }

                var reserva = await _context.Reserva
                    .Include(r => r.Usuario)
                    .Include(r => r.Servicio) // <-- Agrega esto
                    .FirstOrDefaultAsync(r => r.res_id == reservaId);


                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada.";
                    return RedirectToAction("ConsultarServicios");
                }

                var estadoAnterior = reserva.Estado;
                if (estadoAnterior == estado)
                {
                    TempData["Mensaje"] = $"La reserva #{reservaId} ya se encuentra en estado {estado}.";
                    return RedirectToAction("ConsultarServicios");
                }

                reserva.Estado = estado;
                await _context.SaveChangesAsync();

                // Cargar plantilla de correo
                var emailTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Emails", "EstadosReserva.html");
                var emailBody = await System.IO.File.ReadAllTextAsync(emailTemplatePath);

                string subject = $"Estado de tu reserva #{reservaId} actualizado a {estado}";
                string mensaje = "";
                string claseColor = "";
                string mensajeCambio = $"El estado de tu pedido <strong>#{reservaId}</strong> ha cambiado a:";
                // Determinar las clases CSS según el estado
                string stylePendiente = "opacity:0.4; color:#F39C12; font-weight:bold; font-size:14px; padding:0 15px;";
                string styleCompletado = "opacity:0.4; color:#27ae60; font-weight:bold; font-size:14px; padding:0 15px;";
                string styleCancelado = "opacity:0.4; color:#E74C3C; font-weight:bold; font-size:14px; padding:0 15px;";
                string colCancelado = "";

                switch (estado)
                {
                    case EstadoReserva.Pendiente:
                        mensaje = "Tu reserva está pendiente de confirmación. Te contactaremos pronto.";
                        stylePendiente = "opacity:1; color:#F39C12; font-weight:bold; font-size:14px; padding:0 15px;";
                        claseColor = "color: #F39C12;"; // Naranja
                        break;
                    case EstadoReserva.Confirmada:
                        mensaje = "¡Tu reserva ha sido confirmada! Te esperamos en el local.";
                        claseColor = "color:#2980b9;"; // Azul
                        break;
                    case EstadoReserva.Cancelada:
                        mensaje = "Tu reserva ha sido cancelada. Si tienes dudas, contáctanos.";
                        colCancelado = @"<td style=""opacity:1; color:#E74C3C; font-weight:bold; font-size:14px; padding:0 15px;"">
                    <img src=""https://firebasestorage.googleapis.com/v0/b/marimonapp.appspot.com/o/Assest_web%2FPedidos%2Fel-tiempo-de-entrega.png?alt=media&token=3eed4a09-a993-40fc-9989-4abcc4c2359b.jpg""
                        alt=""Cancelado"" width=""50"" height=""50"" style=""display:block; margin:0 auto 10px auto;"">
                    Cancelado
                </td>";
                        claseColor = "color: #E42229;"; ; // Rojo
                        break;
                    case EstadoReserva.Completada:
                        mensaje = "¡Gracias por visitarnos! Tu reserva ha sido completada con éxito.";
                        styleCompletado = "opacity:1; color:#27ae60; font-weight:bold; font-size:14px; padding:0 15px;";
                        claseColor = "color:#27ae60;"; // Verde
                        break;
                }

                emailBody = emailBody
                            .Replace("{{UserName}}", reserva.Usuario?.usu_nombre ?? "Cliente")
                            .Replace("{{LogoUrl}}", "https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png")
                            .Replace("{{ReservaId}}", reservaId.ToString())
                            .Replace("{{NumeroReserva}}", reservaId.ToString())
                            .Replace("{{Servicio}}", reserva.Servicio.ser_nombre) // Asegúrate de tener esto en el modelo
                            .Replace("{{FechaReserva}}", reserva.res_fecha.ToString("dd/MM/yyyy"))
                            .Replace("{{Estado}}", estado.ToString())
                            .Replace("{{MensajeEstado}}", mensaje)
                            .Replace("{{ClaseEstadoTexto}}", claseColor)
                            .Replace("{{HoraReserva}}", reserva.res_hora.ToString(@"hh\:mm"))
                            .Replace("{{StylePendiente}}", stylePendiente)
                            .Replace("{{StyleCompletado}}", styleCompletado)
                            .Replace("{{StyleCancelado}}", styleCancelado)
                            .Replace("{{ColCancelado}}", colCancelado)
                            .Replace("{{CallbackUrl}}", "https://marimon-fjv7.onrender.com/Identity/Account/Manage/Citas");


                // Enviar correo
                if (!string.IsNullOrEmpty(reserva.Usuario?.usu_correo))
                {
                    await emailSender.SendEmailAsync(reserva.Usuario.usu_correo, subject, emailBody);
                    TempData["Mensaje"] = $"La reserva #{reservaId} ha cambiado de {estadoAnterior} a {estado}. Se notificó al usuario.";
                }
                else
                {
                    TempData["Mensaje"] = $"La reserva #{reservaId} ha cambiado de {estadoAnterior} a {estado}. No se pudo notificar (sin correo).";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado de la reserva: {ex.Message}";
                _logger.LogError(ex, "Error en CambiarEstadoReserva"); // <- si tienes ILogger
            }

            return RedirectToAction("ConsultarServicios");
        }

        [HttpGet]
        public JsonResult GetCategorias()
        {
            var categorias = _context.Categorias
                .Select(c => new { id = c.cat_id, nombre = c.cat_nombre })
                .ToList();

            return Json(categorias);
        }

        [HttpGet]
        public JsonResult GetAutopartesPorCategoria(int categoriaId)
        {
            var autopartes = _context.Autopartes
                .Where(a => a.CategoriaId == categoriaId)
                .Select(a => new { id = a.aut_id, aut_nombre = a.aut_nombre })
                .ToList();

            return Json(autopartes);
        }

        public class AsignarAutoparteModel
        {
            public int ordenId { get; set; }
            public int autoparteId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AsignarAutoparte([FromBody] AsignarAutoparteModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            var orden = _context.OrdenTrabajos.Find(model.ordenId);
            if (orden == null) return NotFound();

            orden.AutoparteId = model.autoparteId;
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AsignarPersonal([FromBody] AsignarPersonalViewModel model)
        {
            // Buscar la orden por Id
            var orden = await _context.OrdenTrabajos.FindAsync(model.ordenId);
            if (orden == null)
            {
                return Json(new { success = false, message = "Orden no encontrada" });
            }

            // Buscar el personal por Id (string)
            var personal = await _context.Usuarios.FindAsync(model.personalId);
            if (personal == null)
            {
                // Loguear en consola (puedes cambiar a logger si tienes configurado)
                Console.WriteLine($"No se encontró personal con ID: {model.personalId}");

                return Json(new { success = false, message = "Personal no encontrado" });
            }

            // Si personal encontrado, asignar y guardar
            orden.PersonalId = model.personalId;
            await _context.SaveChangesAsync();

            // También puedes mostrar en consola info de personal encontrado
            Console.WriteLine($"Personal asignado: {personal.usu_nombre} ({personal.usu_correo})");

            // Retornar éxito con correo del personal
            return Json(new { success = true, correo = personal.usu_correo });
        }



        public class AsignarPersonalViewModel
        {
            public int ordenId { get; set; }
            public string personalId { get; set; }
        }

        public async Task<IActionResult> ObtenerPersonalServicio()
        {
            // 1. Obtener los usuarios que tienen el rol 'Personal_Servicio'
            var usersInRole = await _userManager.GetUsersInRoleAsync("Personal_Servicio");

            // 2. Obtener los IDs de esos usuarios (estos IDs deben coincidir con 'usu_id' en tu tabla Usuario)
            var userIds = usersInRole.Select(u => u.Id).ToList();

            // 3. Consultar la tabla Usuario para obtener la información detallada
            var personalServicio = _context.Usuarios
                .Where(u => userIds.Contains(u.usu_id))
                .Select(u => new
                {
                    Id = u.usu_id,
                    Nombre = u.usu_nombre,
                    Apellido = u.usu_apellido,
                    Correo = u.usu_correo
                })
                .ToList();

            return Json(personalServicio);
        }

        // Tu acción que retorna la vista con la lista ordenes
        public IActionResult OrdenTrabajo()
        {

            var ordenes = _context.OrdenTrabajos
                .Include(o => o.Reserva)
                    .ThenInclude(r => r.Usuario)
                .Include(o => o.Reserva)
                    .ThenInclude(r => r.Servicio)
                .Include(o => o.Personal)
                .Include(o => o.Autoparte)
                .OrderBy(o => o.OrdenTrabajoId)
                .ToList();

            return View(ordenes);
        }

        // Carga la vista parcial con los servicios
        public IActionResult RegistrarOrdenParcial()
        {
            var servicios = _context.Servicio.ToList();
            return PartialView("_RegistrarOrdenPartial", servicios);
        }

        // Cargar reservas de un servicio
        [HttpGet]
        public JsonResult GetReservasPorServicio(int servicioId)
        {
            var reservas = _context.Reserva
                .Where(r => r.ser_id == servicioId
                         && !_context.OrdenTrabajos.Any(o => o.ReservaId == r.res_id))
                .Select(r => new ReservaDto
                {
                    Id = r.res_id,
                    Fecha = r.res_fecha // Asegúrate que el campo se llama así
                })
                .ToList();

            return Json(reservas);
        }


        public class ReservaDto
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }

        }

        public class RegistrarOrdenDto
        {
            public int ReservaId { get; set; }
            public string? Descripcion { get; set; }
        }


        // Guardar orden con solo el ReservaId
        [HttpPost]
        public IActionResult RegistrarOrden([FromBody] RegistrarOrdenDto dto)
        {
            if (!_context.Reserva.Any(r => r.res_id == dto.ReservaId))
            {
                return Json(new { success = false, message = "Reserva no válida." });
            }

            if (_context.OrdenTrabajos.Any(o => o.ReservaId == dto.ReservaId))
            {
                return Json(new { success = false, message = "Ya existe una orden para esta reserva." });
            }

            var orden = new OrdenTrabajo
            {
                ReservaId = dto.ReservaId,
                Descripcion = dto.Descripcion // Solo se usa en memoria, no se guarda
            };

            _context.OrdenTrabajos.Add(orden);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Orden registrada exitosamente.",
                descripcionUsada = orden.Descripcion
            });
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPdf(int id)
        {
            var orden = await _context.OrdenTrabajos
                .Include(o => o.Reserva)
                    .ThenInclude(r => r.Servicio)
                .Include(o => o.Reserva)
                    .ThenInclude(r => r.Usuario)
                .Include(o => o.Personal)
                .Include(o => o.Autoparte)
                .FirstOrDefaultAsync(o => o.OrdenTrabajoId == id);

            if (orden == null)
                return NotFound();

            // Renderiza la vista HTML a string
            var htmlContent = await RenderViewToStringAsync("OrdenTrabajoPdf", orden);

            // Genera el PDF con DinkToPdf
            var pdf = _pdfService.GenerateComprobantePdf(htmlContent);

            return File(pdf, "application/pdf", $"OrdenTrabajo_{id}.pdf");
        }

        private async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
        {
            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);

            using var sw = new StringWriter();
            var viewResult = _viewEngine.FindView(actionContext, viewName, false);

            if (viewResult.View == null)
                throw new ArgumentNullException($"{viewName} no fue encontrada");

            var viewDictionary = new ViewDataDictionary<TModel>(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(HttpContext, _tempDataProvider);
            var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, tempData, sw, new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        public IActionResult TestPdf()
        {
            string html = "<h1>Hola PDF</h1><p>Esto es una prueba.</p>";
            var pdf = _pdfService.GenerateComprobantePdf(html);
            return File(pdf, "application/pdf", "prueba.pdf");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}