using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Marimon.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

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

        public IActionResult AgregarAlCarrito(int id)
        {
            // Buscar la autoparte en la base de datos
            var autoparte = _context.Autopartes.FirstOrDefault(a => a.aut_id == id);
            if (autoparte == null)
            {
                return NotFound();
            }

            // Obtener el carrito actual de la sesión
            var carrito = ObtenerCarritoDeSesion();

            // Verificar si el artículo ya está en el carrito
            var itemExistente = carrito.FirstOrDefault(c => c.aut_id == id);
            if (itemExistente != null)
            {
                // Incrementar la cantidad si ya existe
                itemExistente.aut_cantidad++;
            }
            else
            {
                // Agregar un nuevo artículo al carrito
                carrito.Add(new Autoparte
                {
                    aut_id = autoparte.aut_id,
                    aut_nombre = autoparte.aut_nombre,
                    aut_precio = autoparte.aut_precio,
                    aut_cantidad = 1
                });
            }

            // Guardar el carrito actualizado en la sesión
            GuardarCarritoEnSesion(carrito);

            // Redirigir al índice del catálogo o a otra página
            return RedirectToAction("Index");
        }

        public List<Autoparte> ObtenerCarritoDeSesion()
        {
            var carritoJson = HttpContext.Session.GetString("Carrito");
            return string.IsNullOrEmpty(carritoJson)
                ? new List<Autoparte>()
                : JsonSerializer.Deserialize<List<Autoparte>>(carritoJson);
        }

        private void GuardarCarritoEnSesion(List<Autoparte> carrito)
        {
            var carritoJson = JsonSerializer.Serialize(carrito);
            HttpContext.Session.SetString("Carrito", carritoJson);
        }
    }
}
