using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Marimon.Data;
using Marimon.Models;
using Marimon.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;



namespace Marimon.Controllers
{

    public class CarritoController : Controller
    {
        private readonly ILogger<CarritoController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CarritoController(UserManager<IdentityUser> userManager, ApplicationDbContext context, ILogger<CarritoController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }


        public async Task<IActionResult> Index()
        {
            var identityUserId = _userManager.GetUserId(User);

            if (identityUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);
            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var carrito = await _context.Carritos
                .Include(c => c.CarritoAutopartes)
                    .ThenInclude(ca => ca.Autoparte)
                        .ThenInclude(a => a.Ofertas)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

            if (carrito == null)
            {
                carrito = new Carrito { CarritoAutopartes = new List<CarritoAutoparte>() };
            }

            return View(carrito);
        }


        // M�todo para mostrar el sidebar del carrito
        public async Task<IActionResult> Side()
        {
            var identityUserId = _userManager.GetUserId(User);

            if (identityUserId == null)
            {
                // Usuario no logueado, retornar carrito vacío
                return PartialView("_Side", new Carrito { CarritoAutopartes = new List<CarritoAutoparte>() });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);

            if (usuario == null)
            {
                return PartialView("_Side", new Carrito { CarritoAutopartes = new List<CarritoAutoparte>() });
            }

            var carrito = await _context.Carritos
                .Include(c => c.CarritoAutopartes)
                    .ThenInclude(ca => ca.Autoparte)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

            return PartialView("_Side", carrito ?? new Carrito { CarritoAutopartes = new List<CarritoAutoparte>() });
        }

        [HttpPost]
        public async Task<IActionResult> AñadirAlCarrito(int autoparteId, int cantidad = 1)
        {
            try
            {
                // Buscar el autoparte
                var autoparte = await _context.Autopartes
                    .Include(a => a.Ofertas)
                    .FirstOrDefaultAsync(a => a.aut_id == autoparteId);

                decimal precioFinal = autoparte.aut_precio;
                var hoy = DateTime.Now;

                var ofertaActiva = autoparte.Ofertas?
                    .FirstOrDefault(o => o.ofe_activa && o.ofe_fecha_inicio <= hoy && o.ofe_fecha_fin >= hoy);

                if (ofertaActiva != null)
                {
                    precioFinal = autoparte.aut_precio - (autoparte.aut_precio * ofertaActiva.ofe_porcentaje / 100);
                }


                if (autoparte == null)
                {
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? Json(new { success = false, message = "Producto no encontrado" })
                        : NotFound();
                }

                // Obtener el usu_id a partir del IdentityUser logueado
                var identityUserId = _userManager.GetUserId(User);
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);
                if (usuario == null)
                {
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? Json(new { success = false, message = "Usuario no autenticado" })
                        : Unauthorized();
                }

                var userId = usuario.usu_id;

                // Buscar o crear el carrito del usuario
                var carrito = await _context.Carritos
                    .Include(c => c.CarritoAutopartes)
                    .FirstOrDefaultAsync(c => c.UsuarioId == userId);

                if (carrito == null)
                {
                    carrito = new Carrito { UsuarioId = userId, CarritoAutopartes = new List<CarritoAutoparte>() };
                    _context.Carritos.Add(carrito);
                }

                // Buscar si el autoparte ya está en el carrito
                var carritoProducto = carrito.CarritoAutopartes.FirstOrDefault(cp => cp.AutoparteId == autoparteId);
                if (carritoProducto != null)
                {
                    carritoProducto.car_cantidad += cantidad;
                    carritoProducto.car_subtotal = carritoProducto.car_cantidad * precioFinal;
                }
                else
                {
                    carrito.CarritoAutopartes.Add(new CarritoAutoparte
                    {
                        AutoparteId = autoparteId,
                        car_cantidad = cantidad,
                        car_subtotal = cantidad * precioFinal,
                        Autoparte = autoparte,
                    });
                }

                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = "Producto añadido al carrito",
                        cantidadTotal = carrito.CarritoAutopartes.Sum(ca => ca.car_cantidad)
                    });
                }

                return RedirectToAction("Index", "Catalogo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al añadir producto al carrito.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Error interno del servidor" });
                }
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        // [HttpGet]
        // public IActionResult GetCarritoCount()
        // {
        //     // Obtener el ID del usuario actual
        //     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //     // Asegúrate de que el modelo 'Carrito' usa 'UsuarioId' en lugar de 'UserId'
        //     var count = _context.CarritoAutopartes
        //                         .Include(cp => cp.Carrito) // Asegúrate de incluir el carrito para acceder a 'UsuarioId'
        //                         .Where(cp => cp.Carrito.UsuarioId == userId) // Cambiar 'UserId' por 'UsuarioId'
        //                         .Sum(cp => cp.car_cantidad);

        //     return Json(new { count });
        // }

        [HttpPost]
        public async Task<IActionResult> ActualizarCantidad([FromBody] ActualizarCantidadRequest request)
        {
            var carrito = await _context.Carritos
                .Include(c => c.CarritoAutopartes)
                    .ThenInclude(ca => ca.Autoparte)
                        .ThenInclude(a => a.Ofertas)
                .FirstOrDefaultAsync(c => c.car_id == request.CarritoId);

            if (carrito == null)
                return NotFound(new { Message = "Carrito no encontrado", request.CarritoId });

            var carritoAutoparte = carrito.CarritoAutopartes.FirstOrDefault(ca => ca.carAut_id == request.ProductoId);
            if (carritoAutoparte == null)
                return NotFound(new { Message = "CarritoAutoparte no encontrado", request.ProductoId });

            var autoparte = carritoAutoparte.Autoparte;

            decimal precioBase = autoparte.aut_precio;

            // Buscar oferta activa si existe
            var ofertaActiva = autoparte.Ofertas?.FirstOrDefault(o => o.ofe_activa) ?? null;

            if (ofertaActiva != null)
            {
                // Aplica descuento
                precioBase = precioBase * (1 - (ofertaActiva.ofe_porcentaje / 100));
            }

            carritoAutoparte.car_cantidad = request.NuevaCantidad;

            // Actualiza subtotal según precio con o sin oferta
            carritoAutoparte.car_subtotal = precioBase * request.NuevaCantidad;

            await _context.SaveChangesAsync();

            // Calcula total usando la propiedad calculada car_total (no se asigna, solo se lee)
            decimal total = carrito.car_total;

            return Json(new { subtotal = carritoAutoparte.car_subtotal, total = total });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarProducto([FromBody] EliminarProductoRequest request)
        {
            try
            {
                var carrito = await _context.Carritos
                    .Include(c => c.CarritoAutopartes)
                    .FirstOrDefaultAsync(c => c.car_id == request.CarritoId);

                if (carrito == null)
                    return NotFound(new { message = "Carrito no encontrado", request.CarritoId });

                var carritoAutoparte = carrito.CarritoAutopartes
                    .FirstOrDefault(ca => ca.carAut_id == request.ProductoId);

                if (carritoAutoparte != null)
                {
                    carrito.CarritoAutopartes.Remove(carritoAutoparte);
                    await _context.SaveChangesAsync();
                }

                // Verificar si el carrito quedó vacío
                if (!carrito.CarritoAutopartes.Any())
                {
                    _context.Carritos.Remove(carrito);
                    await _context.SaveChangesAsync();

                    return Json(new { total = 0, carritoEliminado = true });
                }

                return Json(new { total = carrito.car_total, carritoEliminado = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto del carrito");
                return StatusCode(500, "Error interno del servidor");
            }
        }


[HttpPost]
public async Task<IActionResult> AplicarCupon([FromBody] AplicarCuponRequest request)
{
    try
    {
        var identityUserId = _userManager.GetUserId(User);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == identityUserId);

        if (usuario == null)
        {
            return Json(new { success = false, message = "Usuario no autenticado" });
        }

        // Buscar el cupón
        var cupon = await _context.CodigosDescuento
            .Include(c => c.Autoparte)
            .FirstOrDefaultAsync(c => c.cod_codigo == request.CodigoCupon);

        if (cupon == null)
        {
            return Json(new { success = false, message = "Código de descuento no válido" });
        }

        // SOLO VERIFICAR SI YA FUE UTILIZADO - NO CAMBIAR EL ESTADO
        if (cupon.cod_utilizado)
        {
            return Json(new { success = false, message = "Este código ya ha sido utilizado" });
        }

        // Verificar si no ha expirado
        if (cupon.cod_fecha_expiracion < DateTime.Now)
        {
            return Json(new { success = false, message = "Este código ha expirado" });
        }

        // Obtener el carrito del usuario
        var carrito = await _context.Carritos
            .Include(c => c.CarritoAutopartes)
                .ThenInclude(ca => ca.Autoparte)
                    .ThenInclude(a => a.Ofertas)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuario.usu_id);

        if (carrito == null || !carrito.CarritoAutopartes.Any())
        {
            return Json(new { success = false, message = "El carrito está vacío" });
        }

        var hoy = DateTime.Now;
        var productosConDescuento = new List<object>();
        bool cuponAplicado = false;

        // CALCULAR DESCUENTOS SIN MODIFICAR LA BASE DE DATOS
        foreach (var item in carrito.CarritoAutopartes)
        {
            var autoparte = item.Autoparte;

            // Verificar si el producto tiene oferta activa
            var tieneOfertaActiva = autoparte.Ofertas?.Any(o => o.ofe_activa &&
                o.ofe_fecha_inicio <= hoy && o.ofe_fecha_fin >= hoy) ?? false;

            // Solo aplicar cupón si no tiene oferta activa
            if (!tieneOfertaActiva)
            {
                // Si el cupón es específico para un producto, verificar coincidencia
                if (cupon.AutoparteId.HasValue && cupon.AutoparteId.Value != autoparte.aut_id)
                {
                    continue; // Este cupón no aplica para este producto
                }

                // Calcular nuevo precio con descuento (SOLO PARA MOSTRAR)
                var precioOriginal = autoparte.aut_precio;
                var descuento = precioOriginal * (cupon.cod_porcentaje / 100);
                var precioConDescuento = precioOriginal - descuento;
                var nuevoSubtotal = precioConDescuento * item.car_cantidad;

                productosConDescuento.Add(new
                {
                    productoId = item.carAut_id,
                    precioOriginal = precioOriginal,
                    precioConDescuento = precioConDescuento,
                    nuevoSubtotal = nuevoSubtotal
                });

                cuponAplicado = true;
            }
        }

        if (!cuponAplicado)
        {
            return Json(new { success = false, message = "Este cupón no es aplicable a los productos en tu carrito o ya tienen ofertas activas" });
        }

        // NO GUARDAR CAMBIOS EN LA BASE DE DATOS - SOLO MOSTRAR DESCUENTOS
        // await _context.SaveChangesAsync(); // COMENTADO

        // Calcular nuevo total
        var nuevoTotal = productosConDescuento.Sum(p => (decimal)p.GetType().GetProperty("nuevoSubtotal").GetValue(p)) +
                        carrito.CarritoAutopartes.Where(ca => !productosConDescuento.Any(p => 
                            (int)p.GetType().GetProperty("productoId").GetValue(p) == ca.carAut_id))
                        .Sum(ca => ca.car_subtotal);

        return Json(new
        {
            success = true,
            message = "Cupón aplicado exitosamente",
            productosConDescuento = productosConDescuento,
            total = nuevoTotal,
            porcentajeDescuento = cupon.cod_porcentaje
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al aplicar cupón de descuento");
        return Json(new { success = false, message = "Error interno del servidor" });
    }
}

public class AplicarCuponRequest
{
    public string CodigoCupon { get; set; } = string.Empty;
}

    }
}