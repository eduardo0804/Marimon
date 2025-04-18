// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Marimon.Data;
using Marimon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Marimon.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public RegisterConfirmationModel(UserManager<IdentityUser> userManager, IEmailSender emailSender, ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }


        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool DisplayConfirmAccountLink { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string EmailConfirmationUrl { get; set; }
        
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }
            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con el correo '{email}'.");
            }

            Email = email;
            // Cambiamos a false porque estamos usando un servicio de correo real
            DisplayConfirmAccountLink = false;

            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            // Obtener el correo del formulario
            string email = Request.Form["Email"].ToString();
            
            if (string.IsNullOrEmpty(email))
            {
                StatusMessage = "Error: El correo electrónico es requerido.";
                return Page();
            }
            
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // No indicamos que el usuario no existe por razones de seguridad
                ModelState.AddModelError(string.Empty, "No se encontró un usuario con ese correo electrónico.");
                return Page();
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usu_id == user.Id);
            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "No se encontró información adicional del usuario.");
                return Page();
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            // Ruta a la plantilla HTML
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Shared", "email.html");
            var emailTemplate = await System.IO.File.ReadAllTextAsync(templatePath);

            var logoUrl = "https://firebasestorage.googleapis.com/v0/b/marimonapp.appspot.com/o/Assest_web%2Flogo-web-marimon.png?alt=media&token=e7fd3cab-30b0-4a6f-a675-30b3b69f836b";

            var emailBody = emailTemplate
                .Replace("{{LogoUrl}}", logoUrl)
                .Replace("{{UserName}}", $"{usuario.usu_nombre} {usuario.usu_apellido}") // Usa el nombre completo del usuario
                .Replace("{{CallbackUrl}}", HtmlEncoder.Default.Encode(callbackUrl));

            await _emailSender.SendEmailAsync(email, "Reenvío de confirmación de registro", emailBody);

            StatusMessage = "El correo de confirmación ha sido reenviado. Por favor, revisa tu bandeja de entrada.";
            return Page();
        }
    }
}