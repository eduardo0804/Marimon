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
    public class Pedidos : PageModel
    {
        private readonly ILogger<Pedidos> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public Pedidos(ILogger<Pedidos> logger,
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
        public List<Venta> PedidosUsuario { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            PedidosUsuario = await _context.Venta
            .Where(venta => venta.UsuarioId == user.Id)
            .Include(venta => venta.Detalles)
                .ThenInclude(detalle => detalle.Autoparte)
            .Include(venta => venta.MetodoPago)
            .OrderByDescending(venta => venta.ven_fecha)
            .ToListAsync();

            return Page();
        }
    }
}