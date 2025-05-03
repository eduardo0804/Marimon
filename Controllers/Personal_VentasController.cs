using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Marimon.Data;
using Marimon.Models;

namespace Marimon.Controllers
{
    public class Personal_VentasController : Controller
    {
        private readonly ILogger<Personal_VentasController> _logger;
        private readonly ApplicationDbContext _context;

        public Personal_VentasController(ILogger<Personal_VentasController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Redireccionar directamente a la vista de Entradas
            return RedirectToAction("Entradas");
        }

        // GET: Personal_Ventas/Entradas - Página de entrada de productos
        public ActionResult Entradas()
        {
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");

            // Cargar la lista de entradas para mostrar en la tabla
            var listaEntradas = _context.Entradas
                .Include(e => e.Autoparte)
                .OrderByDescending(e => e.ent_id)
                .Take(10)  // Limitar a las últimas 10 entradas
                .ToList();

            ViewBag.ListaEntradas = listaEntradas;

            return View("~/Views/Flujos/Entradas.cshtml");
        }

        // AJAX - Obtener productos por categoría
        [HttpGet]
        public IActionResult ObtenerProductosPorCategoria(int categoriaId)
        {
            try
            {
                // Usar una clase auxiliar en lugar de un tipo anónimo
                var productos = _context.Autopartes
                    .Where(p => p.CategoriaId == categoriaId)
                    .Select(p => new ProductoDTO
                    {
                        aut_id = p.aut_id,
                        aut_nombre = p.aut_nombre ?? string.Empty
                    })
                    .ToList();

                return Json(productos);
            }
            catch (Exception ex)
            {
                var errorResponse = new Dictionary<string, string> { { "error", "Error al obtener productos" } };
                return Json(errorResponse);
            }
        }

        // Clase DTO para evitar tipos anónimos
        public class ProductoDTO
        {
            public int aut_id { get; set; }
            public string aut_nombre { get; set; } = string.Empty;
        }

        // POST: Personal_Ventas/RegistrarEntrada - Procesar entrada de productos
        [HttpPost]
        public IActionResult RegistrarEntrada(int AutoparteId, int ent_cantidad, string ent_proveedor = "")
        {
            try
            {
                // Validar entradas
                if (AutoparteId <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un producto válido.";
                    return RedirectToAction("Entradas");
                }

                if (ent_cantidad <= 0)
                {
                    TempData["Error"] = "La cantidad debe ser mayor a 0.";
                    return RedirectToAction("Entradas");
                }

                // Buscar la autoparte correspondiente
                var autoparte = _context.Autopartes.Find(AutoparteId);
                if (autoparte == null)
                {
                    TempData["Error"] = "No se encontró el producto seleccionado.";
                    return RedirectToAction("Entradas");
                }

                // Crear el registro de entrada
                var entrada = new Entradas
                {
                    AutoparteId = AutoparteId,
                    ent_cantidad = ent_cantidad,
                    ent_proveedor = ent_proveedor,
                    ent_fechaent = DateOnly.FromDateTime(DateTime.Now),
                };

                autoparte.aut_cantidad += ent_cantidad;

                _context.Entradas.Add(entrada);
                _context.Autopartes.Update(autoparte);
                _context.SaveChanges();

                TempData["Mensaje"] = $"Se han registrado {ent_cantidad} unidades de {autoparte.aut_nombre} correctamente.";
                return RedirectToAction("Entradas");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar entrada: {ex.Message}");
                TempData["Error"] = "Error al registrar la entrada: " + ex.Message;
                return RedirectToAction("Entradas");
            }
        }

        // GET: Personal_Ventas/Salidas - Página de salida de productos
        public ActionResult Salidas()
        {
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");

            // Cargar la lista de entradas para mostrar en la tabla
            var listaSalidas = _context.Salida
                .Include(e => e.Autoparte)
                .OrderByDescending(e => e.sal_id)
                .Take(10)  // Limitar a las últimas 10 entradas
                .ToList();

            ViewBag.ListaSalidas = listaSalidas;
            return View("~/Views/Flujos/Salida.cshtml");
        }

        // POST: Personal_Ventas/RegistrarSalida - Procesar salida de productos
        [HttpPost]
        public async Task<IActionResult> RegistrarSalida(int AutoparteId, int sal_cantidad)
        {
            try
            {
                // Validar entradas
                if (AutoparteId <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un producto válido.";
                    return RedirectToAction("Salidas");
                }

                if (sal_cantidad <= 0)
                {
                    TempData["Error"] = "La cantidad debe ser mayor a 0.";
                    return RedirectToAction("Salidas");
                }

                // Buscar la autoparte correspondiente
                var autoparte = await _context.Autopartes.FindAsync(AutoparteId);
                if (autoparte == null)
                {
                    TempData["Error"] = "No se encontró el producto seleccionado.";
                    return RedirectToAction("Salidas");
                }
                else if (autoparte.aut_cantidad < sal_cantidad)
                {
                    TempData["Error"] = "No hay suficiente inventario para realizar la salida.";
                    return RedirectToAction("Salidas");
                }

                // Validar stock disponible
                if (autoparte.aut_cantidad < sal_cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente. Solo hay {autoparte.aut_cantidad} unidades disponibles.";
                    return RedirectToAction("Salidas");
                }

                // Paso 2: Crear el método de pago (en efectivo)
                var metodoPago = new MetodoPago
                {
                    pag_importe = (sal_cantidad * autoparte.aut_precio).ToString(), // Calcular el importe
                    pag_metodo = "Efectivo",
                    pag_fecha = DateOnly.FromDateTime(DateTime.Now),
                };

                _context.MetodoPago.Add(metodoPago);
                await _context.SaveChangesAsync(); // Guardar y generar el ID de metodoPago

                // Paso 3: Crear la venta
                var venta = new Venta
                {
                    ven_fecha = DateOnly.FromDateTime(DateTime.Now),
                    MetodoPagoId = metodoPago.pag_id  // Relacionar con el metodo de pago ya guardado
                };
                _context.Venta.Add(venta);
                await _context.SaveChangesAsync(); // Guardar y generar el ID de venta

                // Paso 4: Crear el detalle de venta (usando solo la cantidad y el AutoparteId)
                var detalleVenta = new DetalleVentas
                {
                    VentaId = venta.ven_id,  // Relacionar con la venta ya guardada
                    AutoParteId = AutoparteId,
                    det_cantidad = sal_cantidad.ToString()
                };
                _context.DetalleVentas.Add(detalleVenta);
                await _context.SaveChangesAsync(); // Guardar y generar el ID de detalleVenta

                // Paso 5: Crear el comprobante (Boleta)
                var comprobante = new Comprobante
                {
                    com_nombre = "Boleta",  // Definir el tipo de comprobante
                    VentaId = venta.ven_id   // Relacionar con la venta ya guardada
                };
                _context.Comprobante.Add(comprobante);
                await _context.SaveChangesAsync(); // Guardar y generar el ID de comprobante

                // Paso 6: Registrar la salida
                var salida = new Salida
                {
                    AutoparteId = AutoparteId,
                    sal_cantidad = sal_cantidad,
                    sal_fechasalida = DateOnly.FromDateTime(DateTime.Now),
                    ComprobanteId = comprobante.com_id // Relacionar con el comprobante ya guardado
                };
                _context.Salida.Add(salida);

                // Actualizar cantidad de inventario
                autoparte.aut_cantidad -= sal_cantidad;
                _context.Autopartes.Update(autoparte);

                await _context.SaveChangesAsync(); // Guardar todo

                TempData["Mensaje"] = $"Se han registrado la salida de {sal_cantidad} unidades de {autoparte.aut_nombre} correctamente.";
                return RedirectToAction("Salidas");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar salida: {ex.Message}");
                TempData["Error"] = "Error al registrar la salida: " + ex.Message;
                return RedirectToAction("Salidas");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}