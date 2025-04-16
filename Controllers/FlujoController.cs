using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Marimon.Controllers
{
    public class FlujoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FlujoController> _logger;

        public FlujoController(ApplicationDbContext context, ILogger<FlujoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Flujo/Entradas
        public async Task<IActionResult> Entradas()
        {
            try
            {
                // Cargar la lista de categorías para el primer dropdown
                var categorias = await _context.Categorias.ToListAsync();
                ViewBag.Categorias = new SelectList(categorias, "cat_id", "cat_nombre");
                
                // Obtener el historial de entradas (si existe una tabla para esto)
                // var entradas = await _context.Entradas.Include(e => e.Autoparte).ToListAsync();
                // return View(entradas);
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al cargar la vista de Entradas: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = ex.Message });
            }
        }

        // GET: Flujo/ObtenerProductosPorCategoria
        [HttpGet]
        public async Task<IActionResult> ObtenerProductosPorCategoria(int categoriaId)
        {
            try
            {
                // Obtener las autopartes filtradas por categoría
                var autopartes = await _context.Autopartes
                    .Where(a => a.CategoriaId == categoriaId)
                    .Select(a => new { id = a.aut_id, nombre = a.aut_nombre })
                    .ToListAsync();

                return Json(autopartes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener productos por categoría: {ex.Message}");
                return Json(new { error = "Error al cargar los productos" });
            }
        }

        // Modelo para recibir los datos del formulario de entrada
        public class EntradaViewModel
        {
            public int CategoriaId { get; set; }
            public int AutoparteId { get; set; }
            public int Cantidad { get; set; }
        }

        // POST: Flujo/RegistrarEntrada
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEntrada(EntradaViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Aquí implementarías la lógica para registrar la entrada en la base de datos
                    // Por ejemplo, podrías tener una tabla de Entradas o actualizar el stock
                    
                    // Ejemplo (ajustar según tu estructura de base de datos):
                    // var entrada = new Entrada
                    // {
                    //     AutoparteId = modelo.AutoparteId,
                    //     Cantidad = modelo.Cantidad,
                    //     FechaRegistro = DateTime.Now
                    // };
                    // _context.Entradas.Add(entrada);
                    // await _context.SaveChangesAsync();

                    // También podrías actualizar el stock de la autoparte
                    // var autoparte = await _context.Autopartes.FindAsync(modelo.AutoparteId);
                    // if (autoparte != null)
                    // {
                    //     autoparte.Stock += modelo.Cantidad;
                    //     _context.Update(autoparte);
                    //     await _context.SaveChangesAsync();
                    // }

                    TempData["Mensaje"] = "Entrada registrada con éxito";
                    return RedirectToAction(nameof(Entradas));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al registrar entrada: {ex.Message}");
                    ModelState.AddModelError("", "Error al registrar la entrada");
                }
            }

            // Si hay errores, recargar las categorías para el dropdown
            var categorias = await _context.Categorias.ToListAsync();
            ViewBag.Categorias = new SelectList(categorias, "cat_id", "cat_nombre");
            
            return View(nameof(Entradas));
        }
    }
}