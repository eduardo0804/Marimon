using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Marimon.Models;
using System.Security.Claims;
using Marimon.Data;
using Microsoft.AspNetCore.Identity;

namespace Marimon.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class FavoritosModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<FavoritosModel> _logger;

        public FavoritosModel(ApplicationDbContext context, ILogger<FavoritosModel> logger,
                              UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public List<FavoritoDto> Favoritos { get; set; } = new();

        // ==================== VISTA PRINCIPAL DE FAVORITOS ====================
        public async Task<IActionResult> OnGetAsync()
        {
            var usuarioId = GetCurrentUserId();
            Favoritos = await ObtenerFavoritosUsuario(usuarioId);
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
                return Partial("_FavoritosLista", favoritos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar favoritos - Término: {Termino}", termino);
                return Partial("_FavoritosLista", new List<FavoritoDto>());
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
                query = query.Where(f =>
                    EF.Functions.Like(f.Autoparte.aut_nombre, $"%{terminoBusqueda}%") ||
                    EF.Functions.Like(f.Autoparte.aut_descripcion ?? "", $"%{terminoBusqueda}%"));
            }

            return await query
                .OrderByDescending(f => f.fav_id)
                .Select(f => new FavoritoDto
                {
                    Id = f.fav_id,
                    AutoparteId = f.AutoparteId,
                    AutoparteNombre = f.Autoparte.aut_nombre,
                    AutopartePrecio = f.Autoparte.aut_precio,
                    AutoparteDescripcion = f.Autoparte.aut_descripcion,
                    AutoparteImagen = f.Autoparte.aut_imagen,
                    EsFavorito = true
                })
                .ToListAsync();
        }

        // ==================== OBTENER ID DEL USUARIO ACTUAL ====================
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}
