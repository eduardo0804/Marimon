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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class Personal_ServicioController : Controller
    {
        private readonly ILogger<Personal_ServicioController> _logger;
        private readonly ApplicationDbContext _context;

        public Personal_ServicioController(ILogger<Personal_ServicioController> logger, ApplicationDbContext context)
        {
            _context = context;
            _logger = logger;
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

            var reservas = _context.Reserva
                .Include(r => r.Servicio)
                .Include(r => r.Usuario)
                .Where(r =>
                    (serviciosSeleccionados.Count == 0 || serviciosSeleccionados.Contains(r.Servicio.ser_nombre)) &&
                    (!fechaInicio.HasValue || r.res_fecha >= fechaInicio.Value) &&
                    (!fechaFin.HasValue || r.res_fecha <= fechaFin.Value) &&
                    (string.IsNullOrEmpty(estado) || r.Estado.ToString() == estado)
                )
                .OrderBy(r => r.res_fecha)
                .ThenBy(r => r.res_hora)
                .ToList();

            ViewBag.EstadoSeleccionado = estado;
            ViewBag.Estados = reservas.Select(r => r.Estado.ToString()).Distinct().ToList();

            var model = new ServicioReservaViewModel
            {
                Servicios = _context.Servicio.ToList(),
                Reservas = reservas,
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
                            .Replace("{{Servicio}}", reserva.Servicio?.ser_nombre ?? "Servicio reservado") // Asegúrate de tener esto en el modelo
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
            }

            return RedirectToAction("ConsultarServicios");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}