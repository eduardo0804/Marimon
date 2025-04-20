using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Marimon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class CarritoController : Controller
    {
        private readonly ILogger<CarritoController> _logger;

        public CarritoController(ILogger<CarritoController> logger)
        {
            _logger = logger;
        }

        // Método para mostrar el carrito completo
        public IActionResult Index()
        {
            // Obtener el carrito desde la sesión
            var carrito = ObtenerCarritoDeSesion();

            // Pasar el carrito a la vista
            return View(carrito);
        }

        // Método para mostrar el sidebar del carrito
        public IActionResult Side()
        {
            // Obtener el carrito desde la sesión
            var carrito = ObtenerCarritoDeSesion();

            // Pasar el carrito al partial view
            return PartialView("_Side", carrito);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        public List<Autoparte> ObtenerCarritoDeSesion()
        {
            var carritoJson = HttpContext.Session.GetString("Carrito");
            return string.IsNullOrEmpty(carritoJson)
                ? new List<Autoparte>()
                : JsonSerializer.Deserialize<List<Autoparte>>(carritoJson);
        }

    }
}