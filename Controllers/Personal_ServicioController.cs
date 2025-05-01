using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marimon.Controllers
{
    public class Personal_ServicioController : Controller
    {
        private readonly ILogger<Personal_ServicioController> _logger;
        private readonly ApplicationDbContext _context;

        public Personal_ServicioController(ILogger<Personal_ServicioController> logger, ApplicationDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult LServicio()
        {
            var personalServicio = _context.Servicio.ToList();
            return View(personalServicio);
        }
        public IActionResult CServicio()
        {
            var servicios = _context.Servicio.ToList(); 
            return View("CServicio", servicios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CServicio(Servicio servicio, IFormFile imagen)
        {
            try
            {
                if (imagen != null && imagen.Length > 0)
                {
                    var bucket = "marimonapp.appspot.com";
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);

                    var credJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
                    if (string.IsNullOrEmpty(credJson))
                    {
                        throw new Exception("No se encontró la variable de entorno FIREBASE_CREDENTIALS.");
                    }

                    var credential = GoogleCredential.FromJson(credJson)
                        .CreateScoped("https://www.googleapis.com/auth/devstorage.full_control");

                    var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                        using (var stream = imagen.OpenReadStream())
                        {
                            var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name=servicios/{uniqueFileName}";
                            var content = new StreamContent(stream);
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                            var response = await httpClient.PostAsync(url, content);
                            response.EnsureSuccessStatusCode();

                            var publicUrl = $"https://storage.googleapis.com/{bucket}/servicios/{uniqueFileName}";
                            servicio.ser_imagen = publicUrl;
                        }
                    }
                }

                _context.Servicio.Add(servicio);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "El servicio se registró correctamente.";

                return Content("<script>window.parent.location.href = '/Personal_Servicio/LServicio';</script>", "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el servicio");
                ModelState.AddModelError("", $"Error al registrar el servicio: {ex.Message}");
                return View("CrearForm", servicio);
            }
        }
        public IActionResult MServicio()
        {
            return View();
        }
        public IActionResult EServicio()
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