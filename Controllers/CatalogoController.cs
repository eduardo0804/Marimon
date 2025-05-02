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
        public async Task<IActionResult> Index(string buscar)
        {
            var autopartesQuery = _context.Autopartes
                .Include(a => a.Categoria)
                .OrderBy(a => a.aut_id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                var normalizado = buscar.ToLower().Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder();

                foreach (var c in normalizado)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        sb.Append(c);
                }

                normalizado = sb.ToString();

                autopartesQuery = autopartesQuery
                    .Where(a => EF.Functions.ILike(
                        ApplicationDbContext.Unaccent(a.aut_nombre),
                        $"%{normalizado}%"
                    ));
            }

            // Verificar si no hay resultados
            if (!autopartesQuery.Any())
            {
                // Aquí se puede poner el mensaje de "No se encontró ninguna autoparte"
                ViewBag.Mensaje = "No se encontró ninguna autoparte";
            }

            var autopartes = await autopartesQuery.ToListAsync();

            // Obtener el ID del usuario autenticado (ajusta según tu lógica)
            var usuarioId = User.Identity.Name;

            var carrito = await _context.Carritos
                .Include(c => c.CarritoAutopartes)
                .ThenInclude(cp => cp.Autoparte)
                .ThenInclude(a => a.Categoria)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            var model = new CatalogoViewModel
            {
                Autopartes = autopartes,
                Carrito = carrito
            };
            ViewBag.Carrito = carrito;
            return View(model);
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


    }
}