// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Marimon.Data;
using Marimon.Models;

namespace Marimon.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly ApplicationDbContext _context; // Agregamos el contexto de base de datos

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context) // Añadimos el parámetro para el contexto
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
            _context = context; // Inicializamos el contexto
        }

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
        public string ProviderDisplayName { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // Función para limpiar el nombre de usuario
        private string CleanUserName(string userName)
        {
            // Reemplazar caracteres no permitidos con guiones bajos
            return string.Join("", userName.Where(c => char.IsLetterOrDigit(c) || c == '_'));
        }

        // Función para extraer el nombre y apellido de Google
        private (string firstName, string lastName) ExtractNameFromClaims(ClaimsPrincipal principal)
        {
            // Primero intentamos obtener el given_name y family_name (Google proporciona estos)
            string firstName = principal.FindFirstValue(ClaimTypes.GivenName);
            string lastName = principal.FindFirstValue(ClaimTypes.Surname);
            
            // También podemos buscar claims específicos de Google
            if (string.IsNullOrEmpty(firstName))
                firstName = principal.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;
                
            if (string.IsNullOrEmpty(lastName))
                lastName = principal.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value;
            
            // Si no encontramos los claims específicos, usamos el nombre completo como fallback
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                string fullName = principal.FindFirstValue(ClaimTypes.Name) ?? "";
                
                // Dividir el nombre completo en partes
                var nameParts = fullName.Split(' ');
                if (nameParts.Length > 1)
                {
                    if (string.IsNullOrEmpty(firstName))
                        firstName = nameParts[0];
                        
                    if (string.IsNullOrEmpty(lastName))
                        lastName = string.Join(" ", nameParts.Skip(1));
                }
                else
                {
                    // Si solo hay una parte, usamos eso como nombre
                    if (string.IsNullOrEmpty(firstName))
                        firstName = fullName;
                        
                    if (string.IsNullOrEmpty(lastName))
                        lastName = "";
                }
            }
            
            // Para depuración: loguear todos los claims disponibles
            _logger.LogInformation("Claims disponibles del proveedor externo:");
            foreach (var claim in principal.Claims)
            {
                _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
            }
            
            return (firstName ?? "", lastName ?? "");
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // Si el usuario no tiene una cuenta, extraemos la información del proveedor externo
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                
                // Extraer nombre y apellido usando nuestra función
                var (firstName, lastName) = ExtractNameFromClaims(info.Principal);
                
                var cleanedUserName = CleanUserName(email.Split('@')[0]); // Usar parte del email como nombre de usuario
                
                // Crear el nuevo usuario en Identity
                var user = new IdentityUser
                {
                    UserName = cleanedUserName,
                    Email = email,
                    EmailConfirmed = true // Confirmamos automáticamente el correo
                };

                var createResult = await _userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    // Vincular la cuenta externa con el usuario de Identity
                    await _userManager.AddLoginAsync(user, info);
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                    try
                    {
                        // Obtener el ID generado correctamente
                        var userId = await _userManager.GetUserIdAsync(user);
                        
                        // Crear el registro en nuestra tabla personalizada
                        var usuario = new Usuario
                        {
                            usu_id = userId, // Usamos el ID generado por Identity
                            usu_nombre = firstName,
                            usu_apellido = lastName,
                            usu_correo = email,
                            // Otros campos según necesites
                        };
                        
                        _context.Usuarios.Add(usuario);
                        
                        // Guarda los cambios y verifica si hubo éxito
                        var saveResult = await _context.SaveChangesAsync();
                        _logger.LogInformation($"Se guardaron {saveResult} registros en la tabla Usuario para login externo");
                        _logger.LogInformation($"Datos guardados: Nombre={firstName}, Apellido={lastName}, Email={email}");
                        
                        if (saveResult <= 0)
                        {
                            _logger.LogWarning("No se guardó ningún registro en la tabla Usuario para login externo");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Registra cualquier error durante el guardado
                        _logger.LogError(ex, "Error al guardar en la tabla Usuario para login externo");
                    }

                    // Iniciar sesión automáticamente
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ProviderDisplayName = info.ProviderDisplayName;
                ReturnUrl = returnUrl;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                // Extraer nombre y apellido usando nuestra función
                var (firstName, lastName) = ExtractNameFromClaims(info.Principal);

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        try
                        {
                            // Obtener el ID generado correctamente
                            var userId = await _userManager.GetUserIdAsync(user);
                            
                            // Crear el registro en nuestra tabla personalizada
                            var usuario = new Usuario
                            {
                                usu_id = userId,
                                usu_nombre = firstName,
                                usu_apellido = lastName,
                                usu_correo = Input.Email,
                                // Otros campos según necesites
                            };
                            
                            _context.Usuarios.Add(usuario);
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation($"Datos guardados en confirmación: Nombre={firstName}, Apellido={lastName}, Email={Input.Email}");
                        }
                        catch (Exception ex)
                        {
                            // Registra cualquier error durante el guardado
                            _logger.LogError(ex, "Error al guardar en la tabla Usuario durante confirmación externa");
                        }

                        // Confirmar el correo automáticamente
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);

                        // Iniciar sesión automáticamente
                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}