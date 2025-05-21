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
                return RedirectToAction("Login", "Account");

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

                // Preparar contenido del correo (solo descripción)
                string subject = "Nueva reclamación recibida";
                string body = $"Descripción de la reclamación:\n\n{model.Descripcion}";

                // Enviar correo (sin adjuntos)
                await _emailSender.SendEmailAsync("castroadrian1228@gmail.com", subject, body);

                return RedirectToAction("Confirmacion");
            }
            catch (Exception ex)
            {
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
