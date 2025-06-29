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
            var hoy = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

            // 1. Calcular ventas totales una sola vez y convertir a Dictionary para mejor rendimiento
            var ventasTotalesDict = GetVentasTotalesDictionary();

            // 2. Query base SIN Include inicialmente
            var autopartesQuery = _context.Autopartes.AsQueryable();

            // 3. Aplicar filtros ANTES de Include
            if (!string.IsNullOrEmpty(buscar))
            {
                autopartesQuery = autopartesQuery
                    .Where(a => EF.Functions.ILike(a.aut_nombre, $"%{buscar}%")); 
            }

            if (categorias != null && categorias.Any())
            {
                autopartesQuery = autopartesQuery.Where(a => categorias.Contains(a.CategoriaId));
            }

            // 4. Aplicar ordenamiento ANTES de Include
            if (orden == "novedades")
            {
                autopartesQuery = autopartesQuery.OrderByDescending(a => a.aut_id);
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
                var idsVendidos = ventasTotalesDict
                    .OrderByDescending(v => v.Value)
                    .Select(v => v.Key)
                    .Take(100)
                    .ToList();

                autopartesQuery = autopartesQuery
                    .Where(a => idsVendidos.Contains(a.aut_id))
                    .OrderBy(a => idsVendidos.IndexOf(a.aut_id));
            }
            else
            {
                autopartesQuery = autopartesQuery.OrderBy(a => a.aut_id);
            }

            // 5. Contar total ANTES de Include y paginación
            var totalAutopartes = autopartesQuery.Count();

            if (totalAutopartes == 0)
            {
                ViewBag.Mensaje = "No se encontraron productos que coincidan con tu búsqueda.";
                var emptyViewModel = new CatalogoViewModelModificado { Autopartes = new List<AutoparteViewModel>() };
                ViewBag.Categorias = _context.Categorias.ToList();
                return View(emptyViewModel);
            }

            // 6. Aplicar paginación ANTES de Include
            autopartesQuery = autopartesQuery
                .Skip((pagina - 1) * PaginasPorPagina)
                .Take(PaginasPorPagina);

            // 7. Ahora SÍ aplicar Include solo a los registros paginados
            var autopartesConDatos = autopartesQuery
                .Include(a => a.Categoria)
                .Include(a => a.Ofertas.Where(o => o.ofe_fecha_inicio <= hoy && o.ofe_fecha_fin >= hoy))
                .ToList();

            // 8. Mapear a ViewModel optimizado - evitar múltiples OrderByDescending
            var autopartes = autopartesConDatos.Select(a =>
            {
                var ofertaActiva = a.Ofertas?.FirstOrDefault(); 

                return new AutoparteViewModel
                {
                    aut_id = a.aut_id,
                    aut_nombre = a.aut_nombre,
                    aut_descripcion = a.aut_descripcion,
                    aut_precio = a.aut_precio,
                    aut_cantidad = a.aut_cantidad,
                    aut_imagen = a.aut_imagen,
                    CategoriaNombre = a.Categoria?.cat_nombre ?? string.Empty,

                    // Información de ofertas simplificada
                    OfertaId = ofertaActiva?.ofe_id,
                    PorcentajeOferta = ofertaActiva?.ofe_porcentaje,
                    DescripcionOferta = ofertaActiva?.ofe_descripcion,
                    FechaInicio = ofertaActiva?.ofe_fecha_inicio,
                    FechaFin = ofertaActiva?.ofe_fecha_fin,
                    PrecioOferta = ofertaActiva != null
                        ? a.aut_precio - (a.aut_precio * (ofertaActiva.ofe_porcentaje / 100m))
                        : (decimal?)null,
                    OfertaActiva = ofertaActiva != null
                };
            }).ToList();

            var totalPaginas = (int)Math.Ceiling((double)totalAutopartes / PaginasPorPagina);
            var carrito = _context.Carritos.FirstOrDefault(c => c.UsuarioId == User.Identity.Name);

            var viewModel = new CatalogoViewModelModificado
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

        // Método auxiliar para ventas totales
        private Dictionary<int, int> GetVentasTotalesDictionary()
        {
            var ventasOnline = _context.DetalleVentas
                .GroupBy(d => d.AutoParteId)
                .Select(g => new { AutoparteId = g.Key, Cantidad = g.Sum(d => d.det_cantidad) });

            var ventasPresenciales = _context.Salida
                .GroupBy(s => s.AutoparteId)
                .Select(g => new { AutoparteId = g.Key, Cantidad = g.Sum(s => s.sal_cantidad) });

            return ventasOnline
                .Concat(ventasPresenciales)
                .GroupBy(v => v.AutoparteId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
        }

        // GET: Catalogo/DetalleAutoparte/5
        public async Task<IActionResult> DetalleAutoparte(int id)
        {
            Console.WriteLine($"[DEBUG] Detalle recibe aut_id = {id}");
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
                    AutoparteId = r.aut_id,
                    usuario_id = r.UsuarioId,
                    usuario_nombre = _context.Usuario
                        .Where(u => u.usu_id == r.UsuarioId)
                        .Select(u => u.usu_nombre)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Imprime el AutoparteId de la primera reseña (o recórrelas todas)
            if (resenias.Any())
            {
                Console.WriteLine($"[DEBUG] Primera reseña AutoparteId = {resenias[0].AutoparteId}");
            }
            else
            {
                Console.WriteLine("[DEBUG] No hay reseñas para esta autoparte.");
            }

            // Obtener la oferta más reciente
            var oferta = await _context.Ofertas
            .Where(o => o.Autoparte.aut_id == autoparte.aut_id)
                .OrderByDescending(o => o.ofe_id)
                .FirstOrDefaultAsync();

            var hoy = DateTime.Today;
            bool? ofertaActiva = oferta != null && hoy >= oferta.ofe_fecha_inicio && hoy <= oferta.ofe_fecha_fin;
            decimal? precioOferta = (ofertaActiva == true)
                ? autoparte.aut_precio - (autoparte.aut_precio * (oferta.ofe_porcentaje / 100m))
                : (decimal?)null;

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
                Resenias = resenias,

                // Oferta
                OfertaId = oferta?.ofe_id,
                PorcentajeOferta = oferta?.ofe_porcentaje,
                DescripcionOferta = oferta?.ofe_descripcion,
                FechaInicio = oferta?.ofe_fecha_inicio,
                FechaFin = oferta?.ofe_fecha_fin,
                PrecioOferta = precioOferta,
                OfertaActiva = ofertaActiva
            };
            if (User.Identity.IsAuthenticated)
            {
                var usuarioId = _userManager.GetUserId(User);
                modelo.EsFavoritos = await _context.Favoritos
                    .AnyAsync(f => f.UsuarioId == usuarioId && f.AutoparteId == id);
            }
            else
            {
                modelo.EsFavoritos = false;
            }
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
                // Devolver toda la sección de reseñas con el diseño completo
                return await ObtenerSeccionResenias(aut_id);
            }

            return RedirectToAction("Index");
        }

        // Nuevo método para obtener toda la sección de reseñas
        [HttpGet]
        public async Task<IActionResult> ObtenerSeccionResenias(int aut_id)
        {
            // Esto imprimirá en la consola de la aplicación (ej. la ventana de salida en Visual Studio)
            Console.WriteLine($"[DEBUG] ObtenerSeccionResenias recibe aut_id = {aut_id}");
            var resenias = await _context.Resenias
                .Where(r => r.aut_id == aut_id)
                .OrderByDescending(r => r.res_fecha) // Ordenar por fecha descendente
                .Select(r => new ReseniaViewModel
                {
                    res_id = r.res_id,
                    res_comentario = r.res_comentario,
                    res_puntuacion = r.res_puntuacion,
                    res_fecha = r.res_fecha,
                    res_gusto = r.res_gusto,
                    AutoparteId = r.aut_id,
                    usuario_id = r.UsuarioId,
                    usuario_nombre = _context.Usuario
                        .Where(u => u.usu_id == r.UsuarioId)
                        .Select(u => u.usu_nombre)
                        .FirstOrDefault()

                })
                .ToListAsync();

            // Devolver la vista parcial que incluye todo el contenedor de reseñas con estilos
            return PartialView("_SeccionResenias", resenias);
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

            try
            {
                // Buscar autopartes que coincidan con la búsqueda
                var resultados = await _context.Autopartes
                    .Where(a => EF.Functions.ILike(a.aut_nombre.ToLower(), $"%{query.ToLower()}%"))
                    .Select(a => new
                    {
                        id = a.aut_id,
                        nombre = a.aut_nombre,
                        precio = a.aut_precio,
                        imagen = a.aut_imagen
                    })
                    .Take(8)
                    .ToListAsync();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en autocompletado");
                return Json(new List<object>());
            }
        }
        [HttpPost]
        public async Task<IActionResult> AgregarFavorito([FromBody] JsonElement data)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "Debes iniciar sesión." });

            int aut_id = data.GetProperty("aut_id").GetInt32();

            var identityUser = await _userManager.GetUserAsync(User);
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == identityUser.Id);

            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado." });

            var existe = await _context.Favoritos
                .AnyAsync(f => f.UsuarioId == usuario.usu_id && f.AutoparteId == aut_id);

            if (existe)
                return Json(new { success = false, message = "Ya está en favoritos." });

            var favorito = new Favoritos
            {
                UsuarioId = usuario.usu_id,
                AutoparteId = aut_id
            };

            _context.Favoritos.Add(favorito);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Agregado a favoritos." });
        }
        [HttpPost]
        public async Task<IActionResult> QuitarFavorito([FromBody] JsonElement data)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "Debes iniciar sesión." });

            int aut_id = data.GetProperty("aut_id").GetInt32();

            var identityUser = await _userManager.GetUserAsync(User);
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == identityUser.Id);

            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado." });

            var favorito = await _context.Favoritos
                .FirstOrDefaultAsync(f => f.UsuarioId == usuario.usu_id && f.AutoparteId == aut_id);

            if (favorito == null)
                return Json(new { success = false, message = "No está en favoritos." });

            _context.Favoritos.Remove(favorito);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Eliminado de favoritos." });
        }
    }

    // NUEVA CLASE: ViewModel modificado para usar AutoparteViewModel
    public class CatalogoViewModelModificado
    {
        public List<AutoparteViewModel> Autopartes { get; set; } = new List<AutoparteViewModel>();
        public Carrito Carrito { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public string Buscar { get; set; }
    }
}