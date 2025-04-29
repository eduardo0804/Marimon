using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly ILogger<ServiciosController> _logger;
        private readonly ApplicationDbContext _context;

        public ServiciosController(ILogger<ServiciosController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var servicios = _context.Servicio.ToList();
            return View(servicios);
        }

        public IActionResult Detalle(int id)
        {
            var servicio = _context.Servicio.Find(id);
            if (servicio == null)
            {
                return NotFound();
            }
            return View("Servicio", servicio);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}