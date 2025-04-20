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

            return PartialView("_DetalleAutoparteModal", autoparte); // Vista parcial para el modal
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error");
        }
    }
}
