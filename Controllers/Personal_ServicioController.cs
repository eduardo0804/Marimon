using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Marimon.Data;
using Marimon.Enums;
using Marimon.Models;
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

        public IActionResult ConsultarServicios(string nombreServicio, string fechaDesde, string fechaHasta)
        {
            var serviciosSeleccionados = string.IsNullOrEmpty(nombreServicio)
                ? new List<string>()
                : nombreServicio.Split(',').ToList();

            // Conversión segura a DateTime con tipo UTC
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;

            if (DateTime.TryParse(fechaDesde, out var desde))
                fechaInicio = DateTime.SpecifyKind(desde, DateTimeKind.Utc);

            if (DateTime.TryParse(fechaHasta, out var hasta))
                fechaFin = DateTime.SpecifyKind(hasta, DateTimeKind.Utc);

            var reservas = _context.Reserva
                .Include(r => r.Servicio)
                .Include(r => r.Usuario)
                .Select(r => new Reserva
                {
                    res_id = r.res_id,
                    res_placa = r.res_placa,
                    res_fecha = r.res_fecha,
                    res_hora = r.res_hora,
                    Servicio = r.Servicio,
                    Usuario = r.Usuario,
                    Estado = r.Estado // Asegúrate de incluir el Estado aquí
                })
                .Where(r =>
                    (serviciosSeleccionados.Count == 0 || serviciosSeleccionados.Contains(r.Servicio.ser_nombre)) &&
                    (!fechaInicio.HasValue || r.res_fecha >= fechaInicio.Value) &&
                    (!fechaFin.HasValue || r.res_fecha <= fechaFin.Value)
                )
                .OrderBy(r => r.res_fecha) // Ordena por fecha
                .ThenBy(r => r.res_hora) // Y luego por hora
                .ToList();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstadoReserva(int reservaId, string nuevoEstado)
        {
            var reserva = _context.Reserva.FirstOrDefault(r => r.res_id == reservaId);
            if (reserva == null) return NotFound();

            if (Enum.TryParse<EstadoReserva>(nuevoEstado, out var estado))
            {
                reserva.Estado = estado;
                _context.SaveChanges();
                return RedirectToAction("ConsultarServicios"); // o la acción donde muestras la tabla
            }

            return BadRequest("Estado inválido");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}