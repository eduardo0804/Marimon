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
    }
}