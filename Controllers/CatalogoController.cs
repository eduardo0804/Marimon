using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Marimon.Models.ViewModels;

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
        public async Task<IActionResult> Index(string buscar)
        {
            var autopartes = await _context.Autopartes
                .Include(a => a.Categoria)
                .OrderBy(a => a.aut_id)
                .ToListAsync();

            if (!string.IsNullOrEmpty(buscar))
            {
                autopartes = autopartes
                    .Where(a => a.aut_nombre.Contains(buscar))
                    .ToList();
            }

            return View(autopartes);
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
    }
}