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
                .Where(v => v.UsuarioId == user.Id)
                .Join(
                    _context.DetalleVentas,
                    venta => venta.ven_id,
                    detalle => detalle.VentaId,
                    (venta, detalle) => new { venta, detalle }
                )
                .Join(
                    _context.Autopartes,
                    vd => vd.detalle.AutoParteId,
                    autoParte => autoParte.aut_id,
                    (vd, autoParte) => new { vd, autoParte }
                )
                .Select(x => new Venta
                {
                    ven_id = x.vd.venta.ven_id,
                    ven_fecha = x.vd.venta.ven_fecha,
                    AutoParteNombre = x.autoParte.aut_nombre,
                    AutoPartePrecio = x.autoParte.aut_precio,
                    Cantidad = Convert.ToInt32(x.vd.detalle.det_cantidad),
                })
                .ToListAsync();

            return Page();
        }
    }
}