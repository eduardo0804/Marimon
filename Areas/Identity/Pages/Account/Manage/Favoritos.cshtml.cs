using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Marimon.Models;
using System.Security.Claims;
using Marimon.Data;
using Microsoft.AspNetCore.Identity;
using Marimon.ViewModel;

namespace Marimon.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class Favoritos : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<Favoritos> _logger;

        public Favoritos(ApplicationDbContext context, ILogger<Favoritos> logger,
                              UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public FavoritosViewModel FavoritosDM { get; set; } = new();

        // ==================== VISTA PRINCIPAL DE FAVORITOS ====================
        public async Task<IActionResult> OnGetAsync()
        {
            var usuarioId = GetCurrentUserId();
            FavoritosDM.Favoritos = await ObtenerFavoritosUsuario(usuarioId);
            FavoritosDM.TotalFavoritos = FavoritosDM.Favoritos.Count;
            return Page();
        }

        // ==================== ELIMINAR FAVORITO ====================
        public async Task<IActionResult> OnPostEliminarAsync(int autoparteId)
        {
            try
            {
                var usuarioId = GetCurrentUserId();

                var favorito = await _context.Favoritos
                    .FirstOrDefaultAsync(f => f.AutoparteId == autoparteId && f.UsuarioId == usuarioId);

                if (favorito == null)
                {
                    return new JsonResult(new { success = false, message = "No está en favoritos", esFavorito = false });
                }

                _context.Favoritos.Remove(favorito);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Favorito eliminado - Usuario: {UsuarioId}, Autoparte: {AutoparteId}", usuarioId, autoparteId);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = true, message = "Eliminado de favoritos", esFavorito = false });
                }

                TempData["Success"] = "Autoparte eliminada de favoritos";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar favorito - AutoparteId: {AutoparteId}", autoparteId);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, message = "Error interno del servidor" });
                }

                TempData["Error"] = "Error al eliminar de favoritos";
                return RedirectToPage();
            }
        }

        // ==================== BUSCAR FAVORITOS (AJAX) ====================
        public async Task<IActionResult> OnGetBuscarAsync(string? termino)
        {
            try
            {
                var usuarioId = GetCurrentUserId();
                var favoritos = await ObtenerFavoritosUsuario(usuarioId, termino);

                FavoritosDM = new FavoritosViewModel
                {
                    Favoritos = favoritos,
                    TerminoBusqueda = termino,
                    TotalFavoritos = favoritos.Count
                };

                return Partial("FavoritosPartialView", FavoritosDM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar favoritos - Término: {Termino}", termino);
                return Partial("FavoritosPartialView", new FavoritosViewModel());
            }
        }

        // ==================== MÉTODO PRIVADO PARA OBTENER FAVORITOS ====================
        private async Task<List<FavoritoDto>> ObtenerFavoritosUsuario(string usuarioId, string? terminoBusqueda = null)
        {
            var query = _context.Favoritos
                .Include(f => f.Autoparte)
                .Where(f => f.UsuarioId == usuarioId);

            if (!string.IsNullOrWhiteSpace(terminoBusqueda))
            {
                terminoBusqueda = terminoBusqueda.ToLower();

                query = query.Where(f =>
                    EF.Functions.Like(f.Autoparte.aut_nombre, $"%{terminoBusqueda}%") ||
                    (f.Autoparte.aut_descripcion != null &&
                     EF.Functions.Like(f.Autoparte.aut_descripcion, $"%{terminoBusqueda}%"))
                );

            }

            var favoritos = await query
                .OrderByDescending(f => f.fav_id)
                .Select(f => new FavoritoDto
                {
                    Id = f.fav_id,
                    AutoparteId = f.AutoparteId,
                    AutoparteNombre = f.Autoparte.aut_nombre,
                    AutopartePrecio = f.Autoparte.aut_precio,
                    AutoparteDescripcion = f.Autoparte.aut_descripcion ?? "",
                    AutoparteImagen = f.Autoparte.aut_imagen ?? "",
                    EsFavorito = true
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(terminoBusqueda) && favoritos.Count == 0)
            {
                var todosLosFavoritos = await _context.Favoritos
                    .Include(f => f.Autoparte)
                    .Where(f => f.UsuarioId == usuarioId)
                    .OrderByDescending(f => f.fav_id)
                    .Select(f => new FavoritoDto
                    {
                        Id = f.fav_id,
                        AutoparteId = f.AutoparteId,
                        AutoparteNombre = f.Autoparte.aut_nombre,
                        AutopartePrecio = f.Autoparte.aut_precio,
                        AutoparteDescripcion = f.Autoparte.aut_descripcion ?? "",
                        AutoparteImagen = f.Autoparte.aut_imagen ?? "",
                        EsFavorito = true
                    })
                    .ToListAsync();

                var palabras = terminoBusqueda.Split(' ')
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                if (palabras.Any())
                {
                    favoritos = todosLosFavoritos.Where(f =>
                        palabras.Any(palabra =>
                            f.AutoparteNombre.ToLower().Contains(palabra) ||
                            f.AutoparteDescripcion.ToLower().Contains(palabra)
                        )
                    ).ToList();
                }
            }

            return favoritos;
        }

        // ==================== OBTENER ID DEL USUARIO ACTUAL ====================
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}