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

        public IActionResult AgregarAlCarrito(int id)
        {
            // Buscar la autoparte en la base de datos
            var autoparte = _context.Autopartes.FirstOrDefault(a => a.aut_id == id);
            if (autoparte == null)
            {
                return NotFound();
            }

            // Obtener el carrito actual de la sesi�n
            var carrito = ObtenerCarritoDeSesion();

            // Verificar si el art�culo ya est� en el carrito
            var itemExistente = carrito.FirstOrDefault(c => c.aut_id == id);
            if (itemExistente != null)
            {
                // Incrementar la cantidad si ya existe
                itemExistente.aut_cantidad++;
            }
            else
            {
                Console.WriteLine($"Imagen guardada en carrito: {autoparte.aut_imagen}");

                // Agregar un nuevo art�culo al carrito
                carrito.Add(new Autoparte
                {
                    aut_id = autoparte.aut_id,
                    aut_imagen = autoparte.aut_imagen,
                    aut_nombre = autoparte.aut_nombre,
                    aut_precio = autoparte.aut_precio,
                    aut_cantidad = 1
                });
            }

            // Guardar el carrito actualizado en la sesi�n
            GuardarCarritoEnSesion(carrito);

            // Redirigir al �ndice del cat�logo o a otra p�gina
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