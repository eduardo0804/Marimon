using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Marimon.Models;
using Marimon.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Marimon.Controllers
{
    public class TrabajadoresController : Controller
    {
        private readonly ILogger<TrabajadoresController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public TrabajadoresController(
            ILogger<TrabajadoresController> logger,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = new List<UsuarioConRol>();

            // Obtener todos los usuarios
            var users = _userManager.Users.ToList();

            foreach (var user in users)
            {
                // Obtener los roles del usuario
                var userRoles = await _userManager.GetRolesAsync(user);
                var rol = userRoles.FirstOrDefault();
                
                // Solo mostrar usuarios con roles de Personal_Servicio y Personal_Ventas
                if (rol == "Personal_Servicio" || rol == "Personal_Ventas")
                {
                    var usuarioConRol = new UsuarioConRol
                    {
                        Id = user.Id,
                        Email = user.Email,
                        EmailConfirmed = user.EmailConfirmed,
                        LockoutEnabled = user.LockoutEnabled,
                        Rol = rol ?? "Sin Rol"
                    };

                    usuarios.Add(usuarioConRol);
                }
            }

            return View(usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> CrearTrabajador([FromBody] CrearTrabajadorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                // Verificar si el email ya existe
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return Json(new { success = false, errors = new[] { "El correo electrónico ya está registrado." } });
                }

                // Crear nuevo usuario
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    LockoutEnabled = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Asignar rol
                    await _userManager.AddToRoleAsync(user, model.Rol);

                    // Crear registro en tabla Usuario
                    var usuario = new Usuario
                    {
                        usu_id = user.Id,
                        usu_correo = user.Email
                    };

                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Trabajador creado exitosamente." });
                }
                else
                {
                    return Json(new { success = false, errors = result.Errors.Select(e => e.Description) });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear trabajador");
                return Json(new { success = false, errors = new[] { "Error interno del servidor." } });
            }
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerTrabajador(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, error = "Usuario no encontrado." });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var rol = roles.FirstOrDefault();

                var trabajador = new EditarTrabajadorViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,  // Asegurar que no sea null
                    Rol = rol ?? "Sin Rol"
                };

                // Agregar log para debugging
                _logger.LogInformation($"Obteniendo trabajador - ID: {trabajador.Id}, Email: {trabajador.Email}, Rol: {trabajador.Rol}");

                return Json(new { success = true, data = trabajador });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener trabajador");
                return Json(new { success = false, error = "Error interno del servidor." });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> EditarTrabajador([FromBody] EditarTrabajadorViewModel model)
        {
            // Validación personalizada para contraseñas
            if (!string.IsNullOrEmpty(model.Password))
            {
                if (string.IsNullOrEmpty(model.ConfirmPassword) || model.Password != model.ConfirmPassword)
                {
                    return Json(new { success = false, errors = new[] { "Las contraseñas no coinciden." } });
                }
                if (model.Password.Length < 6)
                {
                    return Json(new { success = false, errors = new[] { "La contraseña debe tener al menos 6 caracteres." } });
                }
            }

            // Validar solo el rol obligatorio
            if (string.IsNullOrEmpty(model.Rol))
            {
                return Json(new { success = false, errors = new[] { "El rol es obligatorio." } });
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return Json(new { success = false, errors = new[] { "Usuario no encontrado." } });
                }

                // Cambiar contraseña si se proporcionó una nueva
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    
                    if (!result.Succeeded)
                    {
                        return Json(new { success = false, errors = result.Errors.Select(e => e.Description) });
                    }
                }

                // Cambiar rol si es diferente
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault();
                
                if (currentRole != model.Rol)
                {
                    if (!string.IsNullOrEmpty(currentRole))
                    {
                        await _userManager.RemoveFromRoleAsync(user, currentRole);
                    }
                    await _userManager.AddToRoleAsync(user, model.Rol);
                }

                return Json(new { success = true, message = "Trabajador actualizado exitosamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar trabajador");
                return Json(new { success = false, errors = new[] { "Error interno del servidor." } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EliminarTrabajador([FromBody] EliminarTrabajadorViewModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return Json(new { success = false, error = "Usuario no encontrado." });
                }

                // Eliminar de tabla Usuario primero (debido a la relación)
                var usuario = await _context.Usuarios.FindAsync(model.Id);
                if (usuario != null)
                {
                    _context.Usuarios.Remove(usuario);
                    await _context.SaveChangesAsync();
                }

                // Eliminar usuario de Identity
                var result = await _userManager.DeleteAsync(user);
                
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Trabajador eliminado exitosamente." });
                }
                else
                {
                    return Json(new { success = false, errors = result.Errors.Select(e => e.Description) });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar trabajador");
                return Json(new { success = false, error = "Error interno del servidor." });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }

    // Modelos para las vistas
    public class UsuarioConRol
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public string Rol { get; set; }
    }

    public class CrearTrabajadorViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [RegularExpression("^(Personal_Servicio|Personal_Ventas)$", ErrorMessage = "El rol debe ser Personal_Servicio o Personal_Ventas.")]
        public string Rol { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }
    }

    public class EditarTrabajadorViewModel
    {
        [Required]
        public string Id { get; set; }

        public string Email { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [RegularExpression("^(Personal_Servicio|Personal_Ventas)$", ErrorMessage = "El rol debe ser Personal_Servicio o Personal_Ventas.")]
        public string Rol { get; set; }

        // Removemos [Required] para que sea opcional
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        // Solo validamos Compare si Password no está vacío
        public string ConfirmPassword { get; set; }
    }

    public class EliminarTrabajadorViewModel
    {
        [Required]
        public string Id { get; set; }
    }
}