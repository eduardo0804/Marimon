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
using Microsoft.AspNetCore.Identity;
using Marimon.ViewModel;
namespace Marimon.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CatalogoController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public CatalogoController(ApplicationDbContext context, ILogger<CatalogoController> logger, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // GET: Catalogo/Index
        public IActionResult Index(string buscar, int pagina = 1, string orden = null, List<int> categorias = null)
        {
            const int PaginasPorPagina = 12;

            var ventasOnline = _context.DetalleVentas
                .GroupBy(d => d.AutoParteId)
                .Select(g => new { AutoparteId = g.Key, Cantidad = g.Sum(d => d.det_cantidad) });

            var ventasPresenciales = _context.Salida
                .GroupBy(s => s.AutoparteId)
                .Select(g => new { AutoparteId = g.Key, Cantidad = g.Sum(s => s.sal_cantidad) });

            var ventasTotales = ventasOnline
                .Concat(ventasPresenciales)
                .GroupBy(v => v.AutoparteId)
                .Select(g => new { AutoparteId = g.Key, Total = g.Sum(x => x.Cantidad) })
                .ToList();

            var autopartesQuery = _context.Autopartes.Include(a => a.Categoria).AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                autopartesQuery = autopartesQuery
                    .Where(a => EF.Functions.ILike(ApplicationDbContext.Unaccent(a.aut_nombre), $"%{buscar}%"));
            }

            if (categorias != null && categorias.Any())
            {
                autopartesQuery = autopartesQuery.Where(a => categorias.Contains(a.CategoriaId));
            }

            // Filtro de novedades (mostrar solo los 3 últimos por ID)
            bool esNovedades = orden == "novedades";
            if (esNovedades)
            {
                autopartesQuery = autopartesQuery
                    .OrderByDescending(a => a.aut_id)
                    .Take(3);
            }

            else if (orden == "asc")
            {
                autopartesQuery = autopartesQuery.OrderBy(a => a.aut_precio);
            }
            else if (orden == "desc")
            {
                autopartesQuery = autopartesQuery.OrderByDescending(a => a.aut_precio);
            }
            else if (orden == "mas_vendidas")
            {
                autopartesQuery = autopartesQuery
                    .AsEnumerable()
                    .OrderByDescending(a =>
                        ventasTotales.FirstOrDefault(v => v.AutoparteId == a.aut_id)?.Total ?? 0)
                    .AsQueryable();
            }
            else
            {
                autopartesQuery = autopartesQuery.OrderBy(a => a.aut_id);
            }

            var totalAutopartes = autopartesQuery.Count();

            if (totalAutopartes == 0)
            {
                ViewBag.Mensaje = "No se encontraron productos que coincidan con tu búsqueda.";
            }

            // Paginación (solo si no es novedades)
            if (!esNovedades)
            {
                autopartesQuery = autopartesQuery
                    .Skip((pagina - 1) * PaginasPorPagina)
                    .Take(PaginasPorPagina);
            }

            var autopartes = autopartesQuery.ToList();
            var totalPaginas = esNovedades ? 1 : (int)Math.Ceiling((double)totalAutopartes / PaginasPorPagina);

            var carrito = _context.Carritos.FirstOrDefault(c => c.UsuarioId == User.Identity.Name);

            var viewModel = new CatalogoViewModel
            {
                Autopartes = autopartes,
                Carrito = carrito,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                Buscar = buscar
            };

            ViewBag.Categorias = _context.Categorias.ToList();
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

            // Obtener las reseñas relacionadas con la autoparte
            var resenias = await _context.Resenias
                .Where(r => r.aut_id == id)
                .Select(r => new ReseniaViewModel
                {
                    res_id = r.res_id,
                    res_comentario = r.res_comentario,
                    res_puntuacion = r.res_puntuacion,
                    res_fecha = r.res_fecha,
                    res_gusto = r.res_gusto,
                    usuario_id = r.UsuarioId,
                    usuario_nombre = _context.Usuario
                        .Where(u => u.usu_id == r.UsuarioId)
                        .Select(u => u.usu_nombre)
                        .FirstOrDefault()
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
                ProductosSimilares = productosSimilares,
                Resenias = resenias
            };

            return PartialView("_DetalleAutoparteModal", modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarReseniaAutoparte(int aut_id, int res_puntuacion, string res_gusto, string res_comentario)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para agregar una reseña.";
                return RedirectToAction("Index");
            }

            var identityUser = await _userManager.GetUserAsync(User);
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == identityUser.Id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            var nuevaResenia = new Resenia
            {
                res_puntuacion = res_puntuacion,
                res_gusto = res_gusto,
                res_comentario = res_comentario,
                res_fecha = DateTime.UtcNow,
                UsuarioId = usuario.usu_id,
                aut_id = aut_id,
                ser_id = null
            };

            try
            {
                _context.Resenias.Add(nuevaResenia);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar reseña");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest("Ocurrió un error al agregar la reseña.");
                }
                return BadRequest("Ocurrió un error al agregar la reseña.");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return await ObtenerReseniasAutoparte(aut_id);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerReseniasAutoparte(int aut_id)
        {
            var resenias = await _context.Resenias
                .Where(r => r.aut_id == aut_id)
                .Select(r => new ReseniaViewModel
                {
                    res_id = r.res_id,
                    res_comentario = r.res_comentario,
                    res_puntuacion = r.res_puntuacion,
                    res_fecha = r.res_fecha,
                    res_gusto = r.res_gusto,
                    usuario_id = r.UsuarioId,
                    usuario_nombre = _context.Usuario
                        .Where(u => u.usu_id == r.UsuarioId)
                        .Select(u => u.usu_nombre)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewBag.AutoparteId = aut_id;

            return PartialView("_ReseniasAutoparte", resenias);
        }

        [HttpDelete]
        public async Task<IActionResult> EliminarResenia(int id, int aut_id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { success = false, message = "Usuario no autorizado" });
            }

            var identityUser = await _userManager.GetUserAsync(User);
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == identityUser.Id);

            if (usuario == null)
            {
                return BadRequest(new { success = false, message = "Usuario no encontrado" });
            }

            var resenia = await _context.Resenias.FindAsync(id);

            if (resenia == null)
            {
                return NotFound(new { success = false, message = "Reseña no encontrada" });
            }

            if (resenia.UsuarioId != usuario.usu_id)
            {
                return Unauthorized(new { success = false, message = "No puedes eliminar reseñas de otros usuarios" });
            }

            try
            {
                _context.Resenias.Remove(resenia);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Reseña eliminada correctamente" });
                }

                return RedirectToAction("DetalleAutoparte", new { id = aut_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar reseña");
                return BadRequest(new { success = false, message = "Error al eliminar la reseña" });
            }
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