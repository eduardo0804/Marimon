using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Marimon.Services;

namespace Marimon.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly ILogger<ServiciosController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSenderWithAttachments _emailSender;

        public ServiciosController(ILogger<ServiciosController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailSenderWithAttachments emailSender)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            var servicios = _context.Servicio.ToList();
            return View(servicios);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var servicio = await _context.Servicio
                .Include(s => s.Reservas) // Incluye las reservas relacionadas
                .FirstOrDefaultAsync(s => s.ser_id == id);

            if (servicio == null)
            {
                return NotFound();
            }

            string nombre = "";
            string apellido = "";

            if (User.Identity.IsAuthenticated)
            {
                var identityUser = await _userManager.GetUserAsync(User);
                if (identityUser != null)
                {
                    var usuario = _context.Usuario.FirstOrDefault(u => u.usu_id == identityUser.Id);
                    if (usuario != null)
                    {
                        nombre = usuario.usu_nombre ?? "";
                        apellido = usuario.usu_apellido ?? "";
                    }
                }
            }

            ViewBag.Nombre = nombre;
            ViewBag.Apellido = apellido;

            // Pasar las reservas al ViewBag
            ViewBag.TodasLasReservas = _context.Reserva
            .Include(r => r.Servicio)
            .Select(r => new
            {
                res_fecha = r.res_fecha,
                res_hora = r.res_hora,
                ser_nombre = r.Servicio.ser_nombre
            })
            .ToList();

            return View("Servicio", servicio);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int ser_id, string placa, string telefono, string telefono_internacional, DateTime fecha, TimeSpan hora, string detalle)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return Unauthorized();

            var usuario = _context.Usuario.FirstOrDefault(u => u.usu_id == identityUser.Id);
            if (usuario == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(telefono_internacional) ||
                !System.Text.RegularExpressions.Regex.IsMatch(telefono_internacional, @"^\+\d+\s\d{9,15}$"))
            {
                TempData["ErrorReserva"] = "El teléfono debe tener el formato internacional correcto (ej: +51 932454493).";
                return RedirectToAction("Detalle", new { id = ser_id });
            }

            var fechaReserva = fecha.Date + hora;
            var ahora = DateTime.Now;
            var hoy = ahora.Date;

            if (fecha.DayOfWeek == DayOfWeek.Sunday)
            {
                TempData["ErrorReserva"] = "No se puede reservar los domingos.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }
            if (fechaReserva.Date <= hoy)
            {
                TempData["ErrorReserva"] = "Solo puedes reservar para días posteriores al actual.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }
            if (fechaReserva <= ahora.AddHours(1))
            {
                TempData["ErrorReserva"] = "Solo puedes reservar con al menos 1 hora de anticipación.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }

            var reserva = new Reserva
            {
                res_placa = placa,
                res_telefono = telefono_internacional,
                res_fecha = fecha.ToUniversalTime(),
                res_hora = hora, // Capturar la hora
                UsuarioId = usuario.usu_id,
                ser_id = ser_id,
                res_detalle = detalle
            };

            _context.Reserva.Add(reserva);
            await _context.SaveChangesAsync();
            await EnviarOrdenReservaPorCorreo(reserva);

            TempData["ReservaExitosa"] = true;
            return RedirectToAction("Detalle", new { id = ser_id });
        }

        public async Task EnviarOrdenReservaPorCorreo(Reserva reserva)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == reserva.UsuarioId);
            var servicio = await _context.Servicio.FirstOrDefaultAsync(s => s.ser_id == reserva.ser_id);

            if (usuario == null || servicio == null)
                return;

            var user = await _userManager.FindByIdAsync(usuario.usu_id);
            if (user == null)
                return;

            string detalle = string.IsNullOrWhiteSpace(reserva.res_detalle)
                ? "<span style='color:#888;'>Sin detalles adicionales</span>"
                : reserva.res_detalle;

            string emailBody = System.IO.File.ReadAllText("Views/Emails/reserva.html")
                .Replace("{{LogoUrl}}", "https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png")
                .Replace("{{UserName}}", $"{usuario.usu_nombre} {usuario.usu_apellido}")
                .Replace("{{Servicio}}", servicio.ser_nombre)
                .Replace("{{Fecha}}", reserva.res_fecha.ToString("dd/MM/yyyy"))
                .Replace("{{Hora}}", reserva.res_hora.ToString(@"hh\:mm"))
                .Replace("{{Placa}}", reserva.res_placa)
                .Replace("{{Telefono}}", reserva.res_telefono)
                .Replace("{{Descripcion}}", detalle);

            await _emailSender.SendEmailAsync(user.Email, "Orden de Reserva", emailBody);
        }

        public IActionResult DescargarICS(string titulo, string descripcion, string location, DateTime fecha)
        {
            var fechaInicio = fecha.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var fechaFin = fecha.AddHours(1).ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var ics = $@"BEGIN:VCALENDAR
            VERSION:2.0
            BEGIN:VEVENT
            SUMMARY:{titulo}
            DESCRIPTION:{descripcion}
            LOCATION:{location}
            DTSTART:{fechaInicio}
            DTEND:{fechaFin}
            END:VEVENT
            END:VCALENDAR";

            var bytes = System.Text.Encoding.UTF8.GetBytes(ics);
            return File(bytes, "text/calendar", "reserva.ics");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}