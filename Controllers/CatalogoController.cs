using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ILogger<CatalogoController> _logger;
        
        public CatalogoController(ILogger<CatalogoController> logger)
        {
            _logger = logger;
        }
        
        public IActionResult Index()
        {
            // Aquí podrías cargar datos desde una base de datos
            // Por ahora, creamos datos de ejemplo
            var repuestos = new List<Repuesto>
            {
                new Repuesto { Id = 1, Nombre = "Llanta 1", Precio = 99.90M, Imagen = "~/images/llanta.jpg" },
                new Repuesto { Id = 2, Nombre = "Llanta 2", Precio = 95.50M, Imagen = "~/images/llanta.jpg" },
                new Repuesto { Id = 3, Nombre = "Llanta 3", Precio = 89.50M, Imagen = "~/images/llanta.jpg" },
                new Repuesto { Id = 4, Nombre = "Llanta 4", Precio = 105.50M, Imagen = "~/images/llanta.jpg" },
                new Repuesto { Id = 5, Nombre = "Llanta 5", Precio = 120.00M, Imagen = "~/images/llanta.jpg" },
                new Repuesto { Id = 6, Nombre = "Llanta 6", Precio = 110.50M, Imagen = "~/images/llanta.jpg" }
            };
            
            return View(repuestos);
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
    
    // Modelo simple para los repuestos
    public class Repuesto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public string Imagen { get; set; }
    }
}