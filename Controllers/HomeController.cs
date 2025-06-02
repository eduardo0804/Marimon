using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Marimon.Models;
using Microsoft.AspNetCore.Identity;

namespace Marimon.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<IdentityUser> _userManager;


    public HomeController(ILogger<HomeController> logger, UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
        _logger = logger;
    }


    public async Task<IActionResult> Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Personal_Ventas"))
            {
                return RedirectToAction("Index", "Personal_Ventas");
            }
            else if (roles.Contains("Personal_Servicio"))
            {
                return RedirectToAction("Index", "Personal_Servicio");
            }
        }
        return View();
    }

    [HttpGet("/api/esta-logueado")]
    public IActionResult EstaLogueado()
    {
        return Json(new { logueado = User.Identity.IsAuthenticated });
    }

    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Terminos()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
