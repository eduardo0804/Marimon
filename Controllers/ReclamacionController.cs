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

        // POST: Recibir datos del formulario y guardar la reclamación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearReclamacion(Reclamacion model)
        {
            try
            {
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