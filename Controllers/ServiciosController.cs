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
using Microsoft.AspNetCore.Identity;

namespace Marimon.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly ILogger<ServiciosController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager; 

        public ServiciosController(ILogger<ServiciosController> logger, ApplicationDbContext context,UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var servicios = _context.Servicio.ToList();
            return View(servicios);
        }

        public async Task<IActionResult> Detalle(int id)
    {
        var servicio = _context.Servicio.Find(id);
        if (servicio == null)
        {
            return NotFound();
        }

        string nombre = "";
        string apellido = "";

        if (User.Identity.IsAuthenticated)
    {
        var identityUser = await _userManager.GetUserAsync(User);
        if (identityUser != null)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.usu_id == identityUser.Id);
            if (usuario != null)
            {
                nombre = usuario.usu_nombre ?? "";
                apellido = usuario.usu_apellido ?? "";
            }
        }
    }

        ViewBag.Nombre = nombre;
        ViewBag.Apellido = apellido;

        return View("Servicio", servicio);
    }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}