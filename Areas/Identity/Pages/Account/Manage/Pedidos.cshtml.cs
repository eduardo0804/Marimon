using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Marimon.Models;
using Marimon.ViewModel;
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
            PedidosUsuarioVM = new List<PedidoViewModel>();
        }

        public bool UsesExternalLogin { get; set; }
        public List<PedidoViewModel> PedidosUsuarioVM { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            var pedidos = await _context.Venta
                .Where(venta => venta.UsuarioId == user.Id)
                .Include(venta => venta.Detalles)
                    .ThenInclude(detalle => detalle.Autoparte)
                        .ThenInclude(a => a.Ofertas)
                .Include(venta => venta.MetodoPago)
                .OrderByDescending(venta => venta.ven_fecha)
                .ToListAsync();

            PedidosUsuarioVM = new List<PedidoViewModel>();

            foreach (var pedido in pedidos)
            {
                var pedidoVM = new PedidoViewModel
                {
                    PedidoId = pedido.ven_id,
                    Fecha = pedido.ven_fecha,
                    Estado = pedido.Estado,
                    MetodoPago = pedido.MetodoPago != null ? pedido.MetodoPago.pag_metodo ?? string.Empty : string.Empty,
                    Detalles = new List<PedidoDetalleViewModel>(),
                    Total = 0
                };

                string? codigoDescuento = null;
                // Buscar si la venta tiene un campo de código de descuento
                if (pedido.GetType().GetProperty("CodigoDescuento") != null)
                {
                    codigoDescuento = (string?)pedido.GetType().GetProperty("CodigoDescuento")?.GetValue(pedido);
                }

                foreach (var detalle in pedido.Detalles)
                {
                    var autoparte = detalle.Autoparte;
                    if (autoparte == null) continue;
                    decimal precioFinal = CalcularPrecioFinal(autoparte, codigoDescuento);
                    string tipoDescuento = "Ninguno";
                    string descripcionDescuento = string.Empty;

                    var fechaPedido = pedido.ven_fecha.ToDateTime(TimeOnly.MinValue);
                    var ofertaActiva = autoparte.Ofertas?.FirstOrDefault(o => o.ofe_activa && o.ofe_fecha_inicio <= fechaPedido && o.ofe_fecha_fin >= fechaPedido);
                    if (ofertaActiva != null)
                    {
                        tipoDescuento = "Oferta";
                        descripcionDescuento = ofertaActiva.ofe_descripcion;
                    }
                    // Si tienes lógica de cupones por venta, aquí puedes aplicar el descuento adicional

                    var detalleVM = new PedidoDetalleViewModel
                    {
                        NombreAutoparte = autoparte.aut_nombre ?? string.Empty,
                        Cantidad = detalle.det_cantidad,
                        PrecioUnitarioFinal = Math.Round(precioFinal, 2),
                        SubtotalFinal = Math.Round(precioFinal * detalle.det_cantidad, 2),
                        TipoDescuento = tipoDescuento,
                        DescripcionDescuento = descripcionDescuento,
                        ImagenAutoparte = autoparte.aut_imagen ?? string.Empty
                    };
                    pedidoVM.Detalles.Add(detalleVM);
                    pedidoVM.Total += detalleVM.SubtotalFinal;
                }
                PedidosUsuarioVM.Add(pedidoVM);
            }

            return Page();
        }

        private decimal CalcularPrecioFinal(Autoparte autoparte, string? codigoDescuento = null)
        {
            decimal precio = autoparte.aut_precio;
            var hoy = DateTime.Now;
            // Usar la fecha de la venta para la oferta (no la fecha actual)
            // Pero aquí solo tenemos la autoparte y el código, así que asumimos oferta vigente si existe
            var ofertaActiva = autoparte.Ofertas?.FirstOrDefault(o => o.ofe_activa && o.ofe_fecha_inicio <= hoy && o.ofe_fecha_fin >= hoy);
            if (ofertaActiva != null)
            {
                precio = autoparte.aut_precio * (1 - ofertaActiva.ofe_porcentaje / 100);
            }
            // Lógica de cupones: buscar el cupón por código, sin importar expiración ni si está usado
            if (!string.IsNullOrEmpty(codigoDescuento))
            {
                var cupon = _context.CodigosDescuento.FirstOrDefault(c =>
                    c.cod_codigo == codigoDescuento &&
                    (c.AutoparteId == null || c.AutoparteId == autoparte.aut_id));
                if (cupon != null)
                {
                    precio = precio * (1 - cupon.cod_porcentaje / 100);
                }
            }
            return precio;
        }
    }
}