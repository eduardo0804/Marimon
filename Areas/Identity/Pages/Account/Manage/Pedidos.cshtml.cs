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
            //obtener pedidos desde comprobante, atraves de la tabla boleta-factura
            PedidosUsuario = await _context.Venta
            .Join(
                _context.Comprobante, //tabla comprobante
                venta => venta.ven_id,
                comprobante => comprobante.VentaId,
                (venta, comprobante) => new { venta, comprobante }
            )
            .Join(
                _context.DetalleVentas, //tabla detalle ventas
                vc => vc.venta.ven_id,
                detalle => detalle.VentaId,
                (vc, detalle) => new { vc, detalle }
            )
            .Join(
                _context.Autopartes, //tabla autopartes
                vcd => vcd.detalle.AutoParteId,
                autoParte => autoParte.aut_id,
                (vcd, autoParte) => new { vcd, autoParte }
            )
            .Join(
                _context.MetodoPago, //tabla metodo pago
                vcdap => vcdap.vcd.vc.venta.MetodoPagoId,
                metodoPago => metodoPago.pag_id,
                (vcdap, metodoPago) => new { vcdap, metodoPago }
            )
            // .Where(x =>
            //     _context.Boleta.Any(b => b.UsuarioId == user.Id && b.bol_id == x.vcdap.vcd.vc.comprobante.BoletaId) ||
            //     _context.Factura.Any(f => f.UsuarioId == user.Id && f.fac_id == x.vcdap.vcd.vc.comprobante.FacturaId)
            // )
            .Select(x => new Venta
            {
                ven_id = x.vcdap.vcd.vc.venta.ven_id,
                ven_fecha = x.vcdap.vcd.vc.venta.ven_fecha,
                AutoParteNombre = x.vcdap.autoParte.aut_nombre,
                AutoPartePrecio = x.vcdap.autoParte.aut_precio,
                Cantidad = Convert.ToInt32(x.vcdap.vcd.detalle.det_cantidad),
            })
            .ToListAsync();
            return Page();
        }
    }
}