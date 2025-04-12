using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Marimon.Models;

namespace Marimon.Controllers;

public class ServiciosController : Controller
{
    private readonly ILogger<ServiciosController> _logger;

    public ServiciosController(ILogger<ServiciosController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("~/Views/Servicios/Index.cshtml");
    }

    public IActionResult Servicio()
    {
        return View("~/Views/Servicios/Servicio.cshtml");
    }

    public IActionResult Privacy()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
