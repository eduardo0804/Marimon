// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Marimon.Data;
using Marimon.Models;
using System.Linq;

namespace Marimon.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Display(Name = "Nombres")]
            public string Nombres { get; set; }

            [Display(Name = "Apellidos")]
            public string Apellidos { get; set; }
            
            [Display(Name = "Nombre para Perfil")]
            public string NombrePerfil { get; set; }
            
            [Display(Name = "Email")]
            [EmailAddress]
            public string Email { get; set; }
            
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña Actual")]
            public string ContraseñaActual { get; set; }

            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y como máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nueva Contraseña")]
            public string NuevaContraseña { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Repetir Nueva Contraseña")]
            [Compare("NuevaContraseña", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
            public string ConfirmarContraseña { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            
            // Obtener datos del usuario de la tabla personalizada
            var userId = user.Id;
            var usuario = _context.Usuarios.FirstOrDefault(u => u.usu_id == userId);

            Username = userName;

            Input = new InputModel
            {
                Email = email,
                Nombres = usuario?.usu_nombre ?? "",
                Apellidos = usuario?.usu_apellido ?? "",
                NombrePerfil = usuario?.usu_nombrePerfil ?? ""
            };
        }


        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }
            
            // Verificar que las contraseñas coincidan
            if (!string.IsNullOrEmpty(Input.NuevaContraseña) && Input.NuevaContraseña != Input.ConfirmarContraseña)
            {
                StatusMessage = "Error: La nueva contraseña y la confirmación no coinciden.";
                await LoadAsync(user);
                return RedirectToPage();
            }


            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            
            // Variables para rastrear los tipos de cambios
            bool perfilActualizado = false;
            bool contraseñaActualizada = false;
            
            // Actualizar datos en la tabla personalizada de usuarios
            var userId = user.Id;
            var usuario = _context.Usuarios.FirstOrDefault(u => u.usu_id == userId);
            
            if (usuario != null)
            {
                // Verificar cambios en el perfil
                if (usuario.usu_nombre != Input.Nombres)
                {
                    usuario.usu_nombre = Input.Nombres;
                    perfilActualizado = true;
                }
                
                if (usuario.usu_apellido != Input.Apellidos)
                {
                    usuario.usu_apellido = Input.Apellidos;
                    perfilActualizado = true;
                }
                
                if (usuario.usu_nombrePerfil != Input.NombrePerfil)
                {
                    usuario.usu_nombrePerfil = Input.NombrePerfil;
                    perfilActualizado = true;
                }
                
                // Verificar cambio de correo
                var email = await _userManager.GetEmailAsync(user);
                if (Input.Email != email)
                {
                    // Actualizar email sin requerir confirmación
                    user.Email = Input.Email;
                    user.NormalizedEmail = Input.Email.ToUpper();
                    user.EmailConfirmed = true;
                    
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        StatusMessage = "Error al cambiar el correo electrónico.";
                        return RedirectToPage();
                    }
                    
                    usuario.usu_correo = Input.Email;
                    perfilActualizado = true;
                }
                
                // Guardar cambios si hubo modificaciones en el perfil
                if (perfilActualizado)
                {
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error al actualizar los datos: {ex.Message}";
                        return RedirectToPage();
                    }
                }
            }
            else
            {
                // Si no existe un registro en la tabla Usuarios, crear uno nuevo
                try
                {
                    var nuevoUsuario = new Usuario
                    {
                        usu_id = userId,
                        usu_nombre = Input.Nombres,
                        usu_apellido = Input.Apellidos,
                        usu_correo = Input.Email,
                        usu_nombrePerfil = Input.NombrePerfil
                    };
                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();
                    perfilActualizado = true;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error al crear el registro de usuario: {ex.Message}";
                    return RedirectToPage();
                }
            }
            
            // Cambiar la contraseña si se proporcionó
            if (!string.IsNullOrEmpty(Input.ContraseñaActual) && !string.IsNullOrEmpty(Input.NuevaContraseña))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, 
                    Input.ContraseñaActual, Input.NuevaContraseña);
                
                if (!changePasswordResult.Succeeded)
                {
                    bool contraseñaIncorrecta = false;
                    
                    foreach (var error in changePasswordResult.Errors)
                    {
                        // Verificar si el error está relacionado con contraseña incorrecta
                        if (error.Code == "PasswordMismatch" || 
                            error.Description.Contains("incorrect") || 
                            error.Description.Contains("incorrecta"))
                        {
                            contraseñaIncorrecta = true;
                            // No añadimos el error al ModelState porque queremos manejarlo como StatusMessage
                        }
                        else
                        {
                            // Para otros errores, añadirlos al ModelState
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                    
                    if (contraseñaIncorrecta)
                    {
                        // Establecer mensaje específico para contraseña incorrecta
                        // Agregamos "Error: " al principio para que se muestre en rojo
                        StatusMessage = "Error: La contraseña actual es incorrecta.";
                        return RedirectToPage();
                    }
                    
                    else if (!ModelState.IsValid)
                    {
                        // Para otros errores de cambio de contraseña
                        await LoadAsync(user);
                        return Page();
                    }
                }
                else
                {
                    contraseñaActualizada = true;
                }
            }

            await _signInManager.RefreshSignInAsync(user);

            // Establecer el mensaje según los cambios realizados
            if (perfilActualizado && contraseñaActualizada)
            {
                StatusMessage = "Datos personales y contraseña actualizados";
            }
            else if (perfilActualizado)
            {
                StatusMessage = "Datos del perfil actualizados";
            }
            else if (contraseñaActualizada)
            {
                StatusMessage = "Contraseña cambiada exitosamente";
            }
            else
            {
                StatusMessage = "No se realizaron cambios";
            }

            return RedirectToPage();;
        }

    }
}