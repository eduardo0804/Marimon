using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
            
            // Obtener la lista de autopartes junto con la categoría
            var autopartes = await _context.Autopartes
                .Include(a => a.Categoria)
                .OrderBy(a => a.aut_id)  // Cargar la categoría asociada con la autoparte
                .ToListAsync();

            if (!String.IsNullOrEmpty(buscar))
            {
                // Filtrar la lista de autopartes por el nombre de la autoparte
                autopartes = autopartes.Where(a => a.aut_nombre.Contains(buscar)).ToList();
            }

            // Pasar la lista de autopartes a la vista
            return View(autopartes);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error");
        }
    }
}
