using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            
            // Verificar si el usuario usa login externo
            var logins = await _userManager.GetLoginsAsync(user);
            UsesExternalLogin = logins.Any();
            
            // Obtener datos del usuario de la tabla personalizada
            var userId = user.Id;
            var usuario = _context.Usuarios.FirstOrDefault(u => u.usu_id == userId);

        }
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }
    }
}