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
            ViewBag.UsuarioAutenticado = User.Identity.IsAuthenticated;
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
        public async Task<IActionResult> Reservar(int ser_id, string placa, string telefono,string telefono_internacional, DateTime fecha, TimeSpan hora, string detalle)
        {
            var usuario = await ObtenerUsuarioAutenticado();
            if (usuario == null)
                return Unauthorized();

            // 2. Validar entrada de datos
            if (!ValidarFormatoTelefono(telefono_internacional))
            {
                return MostrarErrorReserva(ser_id, "El teléfono debe tener el formato internacional correcto (ej: +51 932454493).");
            }

            // 3. Validar fecha y hora
            var resultadoValidacion = ValidarFechaHora(fecha, hora);
            if (!resultadoValidacion.esValido)
            {
                return MostrarErrorReserva(ser_id, resultadoValidacion.mensaje);
            }

            // 4. Verificar reserva existente para la misma fecha
            if (await ExisteReservaParaFecha(usuario.usu_id, fecha))
            {
                return MostrarErrorReserva(ser_id, "Ya tienes una reserva programada para esta fecha. Solo puedes realizar una reserva por día.");
            }

            var reserva = CrearNuevaReserva(ser_id, placa, telefono_internacional, fecha, hora, detalle, usuario.usu_id);

            try
            {
                _context.Reserva.Add(reserva);
                await _context.SaveChangesAsync();
                await EnviarOrdenReservaPorCorreo(reserva);

                TempData["ReservaExitosa"] = true;
                return RedirectToAction("Detalle", new { id = ser_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la reserva");
                return MostrarErrorReserva(ser_id, "Ocurrió un error al procesar tu reserva. Por favor, intenta nuevamente.");
            }
        }

        private async Task<Usuario> ObtenerUsuarioAutenticado()
        {
            if (!User.Identity.IsAuthenticated)
                return null;

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return null;

            return _context.Usuario.FirstOrDefault(u => u.usu_id == identityUser.Id);
        }

        private bool ValidarFormatoTelefono(string telefono)
        {
            return !string.IsNullOrWhiteSpace(telefono) &&
                   System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^\+\d+\s\d{9,15}$");
        }

        private (bool esValido, string mensaje) ValidarFechaHora(DateTime fecha, TimeSpan hora)
        {
            var fechaReserva = fecha.Date + hora;
            var ahora = DateTime.Now;
            var hoy = ahora.Date;

            if (fecha.DayOfWeek == DayOfWeek.Sunday)
                return (false, "No se puede reservar los domingos.");

            if (fechaReserva.Date <= hoy)
                return (false, "Solo puedes reservar para días posteriores al actual.");

            if (fechaReserva <= ahora.AddHours(1))
                return (false, "Solo puedes reservar con al menos 1 hora de anticipación.");

            return (true, string.Empty);
        }

        private async Task<bool> ExisteReservaParaFecha(string usuarioId, DateTime fecha)
        {
            var fechaUtc = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
            return await _context.Reserva.AnyAsync(r => 
                r.UsuarioId == usuarioId && 
                r.res_fecha.Date == fechaUtc.Date);
        }

        private Reserva CrearNuevaReserva(int servicioId, string placa, string telefono,
            DateTime fecha, TimeSpan hora, string detalle, string usuarioId)
        {
            return new Reserva
            {
                res_placa = placa,
                res_telefono = telefono,
                res_fecha = fecha.ToUniversalTime(),
                res_hora = hora,
                UsuarioId = usuarioId,
                ser_id = servicioId,
                res_detalle = detalle
            };
        }

        private IActionResult MostrarErrorReserva(int servicioId, string mensaje)
        {
            TempData["ErrorReserva"] = mensaje;
            return RedirectToAction("Detalle", new { id = servicioId });
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