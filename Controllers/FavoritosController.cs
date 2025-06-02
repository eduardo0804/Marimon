using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class FavoritosController : Controller
{
    private readonly IFavoritoService _favoritoService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FavoritosController(IFavoritoService favoritoService, UserManager<ApplicationUser> userManager)
    {
        _favoritoService = favoritoService;
        _userManager = userManager;
    }

    // GET: Favoritos
    public async Task<IActionResult> Index(string buscar)
    {
        var usuarioId = _userManager.GetUserId(User);
        var favoritos = await _favoritoService.ObtenerFavoritosUsuarioAsync(usuarioId, buscar);

        var viewModel = new FavoritosListViewModel
        {
            Favoritos = favoritos,
            TerminoBusqueda = buscar,
            TotalFavoritos = favoritos.Count,
            MensajeEstado = favoritos.Count == 0 ?
                (string.IsNullOrEmpty(buscar) ? "No tienes autopartes en favoritos" : "No se encontraron resultados") :
                null
        };

        return View(viewModel);
    }

    // POST: Agregar a favoritos
    [HttpPost]
    public async Task<IActionResult> AgregarFavorito(int autoparteId)
    {
        var usuarioId = _userManager.GetUserId(User);
        var resultado = await _favoritoService.AgregarAFavoritosAsync(autoparteId, usuarioId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = resultado, message = resultado ? "Agregado a favoritos" : "Error al agregar" });
        }

        if (resultado)
            TempData["Success"] = "Autoparte agregada a favoritos";
        else
            TempData["Error"] = "No se pudo agregar a favoritos";

        return RedirectToAction("Index");
    }

    // POST: Eliminar de favoritos
    [HttpPost]
    public async Task<IActionResult> EliminarFavorito(int autoparteId)
    {
        var usuarioId = _userManager.GetUserId(User);
        var resultado = await _favoritoService.EliminarDeFavoritosAsync(autoparteId, usuarioId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = resultado, message = resultado ? "Eliminado de favoritos" : "Error al eliminar" });
        }

        if (resultado)
            TempData["Success"] = "Autoparte eliminada de favoritos";
        else
            TempData["Error"] = "No se pudo eliminar de favoritos";

        return RedirectToAction("Index");
    }

    // POST: Actualizar favorito
    [HttpPost]
    public async Task<IActionResult> ActualizarFavorito(int favoritoId)
    {
        var usuarioId = _userManager.GetUserId(User);
        var resultado = await _favoritoService.ActualizarFavoritoAsync(favoritoId, usuarioId);

        return Json(new { success = resultado, message = resultado ? "Favorito actualizado" : "Error al actualizar" });
    }

    // GET: Verificar si es favorito (AJAX)
    [HttpGet]
    public async Task<IActionResult> EsFavorito(int autoparteId)
    {
        var usuarioId = _userManager.GetUserId(User);
        var esFavorito = await _favoritoService.EsFavoritoAsync(autoparteId, usuarioId);

        return Json(new { esFavorito });
    }

    // GET: Buscar favoritos (AJAX)
    [HttpGet]
    public async Task<IActionResult> BuscarFavoritos(string termino)
    {
        var usuarioId = _userManager.GetUserId(User);
        var favoritos = await _favoritoService.ObtenerFavoritosUsuarioAsync(usuarioId, termino);

        return PartialView("_FavoritosLista", favoritos);
    }
}

public class ApplicationUser
{
}

public interface IFavoritoService
{
    Task<bool> ActualizarFavoritoAsync(int favoritoId, string? usuarioId);
    Task<bool> AgregarAFavoritosAsync(int autoparteId, string? usuarioId);
    Task<bool> EliminarDeFavoritosAsync(int autoparteId, string? usuarioId);
    Task<bool> EsFavoritoAsync(int autoparteId, string? usuarioId);
    Task<List<FavoritoViewModel>> ObtenerFavoritosUsuarioAsync(string? usuarioId, string buscar);
}