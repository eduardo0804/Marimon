using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class GerenteController : Controller
    {
        private readonly ILogger<GerenteController> _logger;
        
        public GerenteController(ILogger<GerenteController> logger)
        {
            _logger = logger;
        }
        
        public IActionResult Index()
        {
            return View();
        }
        
        // Cambia esto para redireccionar al TrabajadoresController
        public IActionResult Trabajadores()
        {
            return RedirectToAction("Index", "Trabajadores");
        }
        
        public IActionResult Reportes()
        {
            return View();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}