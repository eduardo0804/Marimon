using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marimon.Areas.Identity.Pages.Account.Manage
{
    public class Citas : PageModel
    {
        private readonly ILogger<Citas> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public Citas(ILogger<Citas> logger,
                     UserManager<IdentityUser> userManager,
                     SignInManager<IdentityUser> signInManager,
                     ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }
        public bool UsesExternalLogin { get; set; }
        public List<Reserva> ReservasUsuario { get; set; }

        public async Task<IActionResult> OnGetAsync()

        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            // Obtener las reservas relacionadas al usuario
            ReservasUsuario = await _context.Reserva
                .Where(r => r.UsuarioId == user.Id) // Filtrar por el ID del usuario
                .Include(r => r.Servicio) // Incluir información del servicio relacionado
                .ToListAsync();
            return Page();
        }
        public async Task<IActionResult> OnPostCancelarCitaAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            // Buscar la reserva por ID y verificar que pertenezca al usuario actual
            var reserva = await _context.Reserva
                .FirstOrDefaultAsync(r => r.res_id == id && r.UsuarioId == user.Id);

            if (reserva == null)
            {
                TempData["Error"] = "No se encontró la cita especificada o no tienes permiso para eliminarla.";
                return RedirectToPage();
            }

            try
            {
                _context.Reserva.Remove(reserva);
                await _context.SaveChangesAsync();

                TempData["Success"] = "La cita se eliminó correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la reserva {ReservaId}", id);
                TempData["Error"] = "Ocurrió un error al eliminar la cita. Por favor, intenta nuevamente.";
            }

            return RedirectToPage();
        }
    }
}