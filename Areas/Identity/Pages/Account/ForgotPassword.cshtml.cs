// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Marimon.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // No revelar si el usuario no existe o no está confirmado
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Generar el token de restablecimiento de contraseña
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code, email = Input.Email },
                    protocol: Request.Scheme);

                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Emails", "emailContra.html");
                var emailTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
                var logoUrl = "https://firebasestorage.googleapis.com/v0/b/marimonapp.appspot.com/o/Assest_web%2Flogo-web-marimon.png?alt=media&token=e7fd3cab-30b0-4a6f-a675-30b3b69f836b";

                var emailBody = emailTemplate
                    .Replace("{{UserName}}", user.UserName ?? "Usuario")
                    .Replace("{{CallbackUrl}}", HtmlEncoder.Default.Encode(callbackUrl))
                    .Replace("{{LogoUrl}}", logoUrl);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Restablecer contraseña",
                    emailBody);
                TempData["SuccessMessage"] = "Correo de restablecimiento de contraseña enviado con éxito.";

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}