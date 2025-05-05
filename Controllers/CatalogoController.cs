using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Marimon.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Marimon.Models.ViewModels;
using System.Globalization;
using System.Text;
namespace Marimon.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CatalogoController> _logger;

        public CatalogoController(ApplicationDbContext context, ILogger<CatalogoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Catalogo/Index
        public IActionResult Index(string buscar, int pagina = 1)
        {
            const int PaginasPorPagina = 12;  // Número de autopartes por página

            // Consulta base para las autopartes
            var autopartesQuery = _context.Autopartes.AsQueryable();

            // Filtro por búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                autopartesQuery = autopartesQuery
                    .Where(a => EF.Functions.ILike(ApplicationDbContext.Unaccent(a.aut_nombre), $"%{buscar}%"));
            }

            // Obtener el total de registros
            var totalAutopartes = autopartesQuery.Count();

            // Si no hay resultados, establecer un mensaje
            if (totalAutopartes == 0)
            {
                ViewBag.Mensaje = "No se encontraron productos que coincidan con tu búsqueda.";
            }

            // Obtener las autopartes para la página actual
            var autopartes = autopartesQuery
                .Skip((pagina - 1) * PaginasPorPagina)
                .Take(PaginasPorPagina)
                .ToList();

            // Calcular cuántas páginas hay
            var totalPaginas = (int)Math.Ceiling((double)totalAutopartes / PaginasPorPagina);

            // Obtener el carrito (esto puede depender de tu implementación, por ejemplo, desde la sesión o la base de datos)
            var carrito = _context.Carritos.FirstOrDefault(c => c.UsuarioId == User.Identity.Name); // Ejemplo de cómo obtener el carrito

            // Crear el modelo para la vista
            var viewModel = new CatalogoViewModel
            {
                Autopartes = autopartes,
                Carrito = carrito,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                Buscar = buscar
            };

            return View(viewModel);
        }


        // GET: Catalogo/DetalleAutoparte/5
        public async Task<IActionResult> DetalleAutoparte(int id)
        {
            var autoparte = await _context.Autopartes
                .Include(a => a.Categoria)
                .FirstOrDefaultAsync(a => a.aut_id == id);

            if (autoparte == null)
            {
                return NotFound();
            }

            var productosSimilares = await _context.Autopartes
                .Where(a => a.CategoriaId == autoparte.CategoriaId && a.aut_id != id)
                .Select(a => new AutoparteViewModel
                {
                    aut_id = a.aut_id,
                    aut_nombre = a.aut_nombre,
                    aut_precio = a.aut_precio,
                    aut_cantidad = a.aut_cantidad,
                    aut_imagen = a.aut_imagen
                })
                .ToListAsync();

            var modelo = new AutoparteViewModel
            {
                aut_id = autoparte.aut_id,
                aut_nombre = autoparte.aut_nombre,
                aut_descripcion = autoparte.aut_descripcion,
                aut_especificacion = autoparte.aut_especificacion,
                aut_precio = autoparte.aut_precio,
                aut_cantidad = autoparte.aut_cantidad,
                aut_imagen = autoparte.aut_imagen,
                CategoriaNombre = autoparte.Categoria.cat_nombre,
                ProductosSimilares = productosSimilares
            };

            return PartialView("_DetalleAutoparteModal", modelo);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error");
        }

        // GET: Catalogo/Autocomplete
        [HttpGet]
        public async Task<IActionResult> Autocomplete(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            var autopartes = await _context.Autopartes
                .Where(a => EF.Functions.ILike(ApplicationDbContext.Unaccent(a.aut_nombre), $"%{query}%"))
                .Select(a => new
                {
                    id = a.aut_id,
                    nombre = a.aut_nombre,
                    imagen = a.aut_imagen,
                    precio = a.aut_precio
                })
                .Take(10)
                .ToListAsync();

            return Json(autopartes);
        }


    }
}