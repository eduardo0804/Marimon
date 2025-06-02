using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Marimon.Services;
using System.Text.Json;
using System.Text;

namespace Marimon.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly ILogger<ServiciosController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSenderWithAttachments _emailSender;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelId;
        public ServiciosController(
            ILogger<ServiciosController> logger,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailSenderWithAttachments emailSender,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;

            // Configuración de Google AI
            _httpClient = httpClientFactory.CreateClient("GoogleAI");
            _apiKey = configuration["GoogleAI:ApiKey"];
            _modelId = configuration["GoogleAI:ModelId"] ?? "gemini-1.5-flash";
        }

        public IActionResult Index()
        {
            var servicios = _context.Servicio.ToList();
            return View(servicios);
        }

        public async Task<IActionResult> AnalyzeContent(IFormFile image, string comment)
        {
            // Validación: al menos un campo debe estar completo
            if ((image == null || image.Length == 0) && string.IsNullOrWhiteSpace(comment))
            {
                return Json(new { success = false, result = "Por favor, sube una imagen o escribe un comentario para analizar." });
            }

            try
            {
                var parts = new List<object>();
                var prompt = "";

                // Lista de servicios disponibles para incluir en el prompt
                var availableServices = new[]
                {
            "Sistema de Refrigeración",
            "Aire Acondicionado",
            "Mecatrónica y Electrónica",
            "Mantenimientos y Frenos",
            "Diagnóstico y Scanner",
            "Planchado y Pintura Automotriz",
            "Conversión a GLP",
            "Conversión a GNV"
        };

                var servicesListText = string.Join(", ", availableServices);

                // Configurar el prompt según lo que se está analizando
                if (!string.IsNullOrWhiteSpace(comment) && (image == null || image.Length == 0))
                {
                    prompt = $@"Analiza este comentario sobre un problema automotriz y determina qué servicio es el más adecuado.

SERVICIOS DISPONIBLES: {servicesListText}

FORMATO DE RESPUESTA OBLIGATORIO:
- Problema detectado: [descripción breve]
- Recomendación: [sugerencia específica]

El servicio más adecuado es: [EXACTAMENTE uno de los servicios de la lista o ""Ninguno""]

Si no corresponde a ningún servicio especializado, responde ""Ninguno"".
Sé conciso y directo (máximo 3 frases).";

                    parts.Add(new { text = prompt });
                    parts.Add(new { text = comment });
                }
                else if (string.IsNullOrWhiteSpace(comment) && (image != null && image.Length > 0))
                {
                    prompt = $@"Analiza esta imagen de un vehículo o componente automotriz y determina qué servicio es el más adecuado.

SERVICIOS DISPONIBLES: {servicesListText}

FORMATO DE RESPUESTA OBLIGATORIO:
- Problema detectado: [descripción breve]
- Recomendación: [sugerencia específica]

El servicio más adecuado es: [EXACTAMENTE uno de los servicios de la lista o ""Ninguno""]

Si no corresponde a ningún servicio especializado, responde ""Ninguno"".
Sé conciso y directo (máximo 3 frases).";

                    parts.Add(new { text = prompt });

                    // Convertir la imagen a base64
                    string base64Image;
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        byte[] imageBytes = memoryStream.ToArray();
                        base64Image = Convert.ToBase64String(imageBytes);
                    }

                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = image.ContentType,
                            data = base64Image
                        }
                    });
                }
                else // Ambos campos están presentes
                {
                    prompt = $@"Analiza la combinación de imagen y comentario sobre un problema automotriz.

SERVICIOS DISPONIBLES: {servicesListText}

INSTRUCCIONES:
1. Examina ambos inputs identificando componentes/sistemas afectados y síntomas
2. Determina el servicio más adecuado de la lista proporcionada

FORMATO DE RESPUESTA OBLIGATORIO:
- Problema detectado: [descripción técnica concisa]
- Recomendación: [sugerencia específica]

El servicio más adecuado es: [EXACTAMENTE uno de los servicios de la lista o ""Ninguno""]

REGLAS ESPECIALES:
- Si es mantenimiento básico (inflado de llantas, lavado): responde ""Ninguno""
- Si no coincide con servicios especializados: responde ""Ninguno""
- Usa SOLO los nombres exactos de la lista
- Máximo 3 frases en total
- Prioriza diagnóstico preciso";

                    parts.Add(new { text = prompt });

                    // Agregar la imagen
                    string base64Image;
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        byte[] imageBytes = memoryStream.ToArray();
                        base64Image = Convert.ToBase64String(imageBytes);
                    }

                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = image.ContentType,
                            data = base64Image
                        }
                    });

                    // Agregar el comentario
                    parts.Add(new { text = comment });
                }

                var payload = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = parts
                }
            },
                    generation_config = new
                    {
                        temperature = 0.1,
                        topK = 32,
                        topP = 1,
                        maxOutputTokens = 256
                    }
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var endpoint = $"v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error al llamar a Google AI API: {errorContent}");
                    return Json(new { success = false, result = $"Error en la API de Google AI: {response.StatusCode}" });
                }

                var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                string resultText = "";

                if (responseContent.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var content1) &&
                    content1.TryGetProperty("parts", out var responseParts) &&
                    responseParts.GetArrayLength() > 0 &&
                    responseParts[0].TryGetProperty("text", out var text))
                {
                    resultText = text.GetString();
                }
                else
                {
                    resultText = "No se pudo extraer el texto de la respuesta.";
                }

                // Determinar el servicio adecuado usando la nueva lógica
                var service = DetermineServiceFromAIResponse(resultText);
                if (service != null)
                {
                    var typedService = (dynamic)service;
                    // Formatear el resultado con colores de la paleta
                    string formattedResult = $@"
                <div class='card mb-3'>
                    <div class='card-body'>
                        <h5 class='card-title' style='color: #E42229;'>Análisis</h5>
                        <p class='card-text'>{FormatResultText(resultText)}</p>
                        <h5 class='mt-3' style='color: #E42229;'>Servicio recomendado:</h5>
                        <div class='mt-2'>
                            <strong style='font-style: italic;'>{typedService.name}</strong>
                        </div>
                    </div>
                </div>";

                    return Json(new { success = true, result = formattedResult, url = typedService.url });
                }

                return Json(new { success = true, result = $"<div class='alert alert-info'>Análisis realizado: {resultText}</div><p>No se encontró un servicio adecuado.</p>" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar el contenido");
                return Json(new { success = false, result = $"<div class='alert alert-danger'>Error al analizar: {ex.Message}</div>" });
            }
        }

        // Nuevo método que extrae el servicio directamente de la respuesta de la IA
        private object DetermineServiceFromAIResponse(string aiResponse)
        {
            var services = new[]
            {
        new { id = 4, name = "Sistema de Refrigeración", url = "/Servicios/Detalle/4" },
        new { id = 5, name = "Aire Acondicionado", url = "/Servicios/Detalle/5" },
        new { id = 3, name = "Mecatrónica y Electrónica", url = "/Servicios/Detalle/3" },
        new { id = 6, name = "Mantenimientos y Frenos", url = "/Servicios/Detalle/6" },
        new { id = 7, name = "Diagnóstico y Scanner", url = "/Servicios/Detalle/7" },
        new { id = 8, name = "Planchado y Pintura Automotriz", url = "/Servicios/Detalle/8" },
        new { id = 9, name = "Conversión a GLP", url = "/Servicios/Detalle/9" },
        new { id = 10, name = "Conversión a GNV", url = "/Servicios/Detalle/10" }
    };

            // Buscar la línea que contiene "El servicio más adecuado es:"
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string serviceLine = null;

            foreach (var line in lines)
            {
                if (line.Contains("El servicio más adecuado es:", StringComparison.OrdinalIgnoreCase))
                {
                    serviceLine = line;
                    break;
                }
            }

            if (serviceLine != null)
            {
                // Extraer el nombre del servicio después de los dos puntos
                var colonIndex = serviceLine.LastIndexOf(':');
                if (colonIndex >= 0 && colonIndex < serviceLine.Length - 1)
                {
                    var serviceName = serviceLine.Substring(colonIndex + 1).Trim();

                    // Buscar coincidencia exacta primero
                    var exactMatch = services.FirstOrDefault(s =>
                        string.Equals(s.name, serviceName, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null)
                    {
                        return exactMatch;
                    }

                    // Si no hay coincidencia exacta, buscar coincidencia parcial
                    var partialMatch = services.FirstOrDefault(s =>
                        serviceName.Contains(s.name, StringComparison.OrdinalIgnoreCase) ||
                        s.name.Contains(serviceName, StringComparison.OrdinalIgnoreCase));

                    if (partialMatch != null)
                    {
                        return partialMatch;
                    }
                }
            }

            // Si no encuentra nada específico en la respuesta, usar el método de keywords como fallback
            return DetermineServiceByKeywords(aiResponse);
        }

        // Método auxiliar de keywords como fallback (tu método original simplificado)
        private object DetermineServiceByKeywords(string resultText)
        {
            resultText = resultText.ToLower();

            var services = new[]
            {
        new { id = 4, name = "Sistema de Refrigeración", url = "/Servicios/Detalle/4", keywords = new[] { "refrigeración", "radiador", "mangueras", "enfriamiento", "termostato", "refrigerante", "sobrecalentamiento", "temperatura" } },
        new { id = 5, name = "Aire Acondicionado", url = "/Servicios/Detalle/5", keywords = new[] { "aire acondicionado", "a/c", "clima", "climatización", "compresor", "frío", "temperatura cabina", "ventilación" } },
        new { id = 3, name = "Mecatrónica y Electrónica", url = "/Servicios/Detalle/3", keywords = new[] { "mecatrónica", "electrónica", "componentes", "computadora", "sensores", "ecu", "cableado", "fusibles", "batería", "alternador", "sistema eléctrico" } },
        new { id = 6, name = "Mantenimientos y Frenos", url = "/Servicios/Detalle/6", keywords = new[] { "frenos", "mantenimiento", "pastillas", "discos", "preventivo", "filtros", "aceite", "bujías", "correa", "faja", "balatas", "zapatas" } },
        new { id = 7, name = "Diagnóstico y Scanner", url = "/Servicios/Detalle/7", keywords = new[] { "diagnóstico", "scanner", "fallos", "codes", "códigos", "error", "check engine", "obd", "diagnóstico computarizado", "escaneo" } },
        new { id = 8, name = "Planchado y Pintura Automotriz", url = "/Servicios/Detalle/8", keywords = new[] { "planchado", "pintura", "carrocería", "chapa", "golpe", "abolladura", "rayón", "pulido", "latonería", "acabado" } },
        new { id = 9, name = "Conversión a GLP", url = "/Servicios/Detalle/9", keywords = new[] { "glp", "gas licuado", "gas licuado de petróleo", "conversión gas", "kit gas" } },
        new { id = 10, name = "Conversión a GNV", url = "/Servicios/Detalle/10", keywords = new[] { "gnv", "gas natural", "gas natural vehicular", "conversión gas natural" } },
    };

            var matchCounts = new Dictionary<int, int>();
            foreach (var service in services)
            {
                int count = 0;
                foreach (var keyword in service.keywords)
                {
                    int keywordCount = CountOccurrences(resultText, keyword.ToLower());
                    count += keywordCount;
                }

                if (count > 0)
                {
                    matchCounts.Add(service.id, count);
                }
            }

            if (matchCounts.Count > 0)
            {
                int bestServiceId = matchCounts.OrderByDescending(x => x.Value).First().Key;
                return services.FirstOrDefault(s => s.id == bestServiceId);
            }

            return null;
        }

        // Método auxiliar para contar ocurrencias (mantén tu método original)
        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        // Método para formatear el texto (mantén tu método original)
        private string FormatResultText(string text)
        {
            return text.Replace("\n", "<br>");
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var servicio = await _context.Servicio
                .Include(s => s.Reservas) // Incluye las reservas relacionadas
                .FirstOrDefaultAsync(s => s.ser_id == id);

            if (servicio == null)
                return NotFound();

            // Obtener las reseñas relacionadas al servicio con usuario
            var resenias = await _context.Resenias
                .Where(r => r.ser_id == id)
                .Select(r => new
                {
                    r.res_id,
                    r.res_comentario,
                    r.res_puntuacion,
                    r.res_fecha,
                    r.res_gusto,
                    usuario_id = r.UsuarioId,
                    usuario_nombre = _context.Usuario.Where(u => u.usu_id == r.UsuarioId).Select(u => u.usu_nombre).FirstOrDefault()
                })
                .ToListAsync();

            // Guardamos las reseñas en ViewBag
            ViewBag.Resenias = resenias;

            // Variables para información de usuario
            string usuarioId = "";
            string nombre = "";
            string apellido = "";
            string correo = "";

            if (User.Identity.IsAuthenticated)
            {
                var identityUser = await _userManager.GetUserAsync(User);
                if (identityUser != null)
                {
                    usuarioId = identityUser.Id;
                    var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == identityUser.Id);
                    if (usuario != null)
                    {
                        nombre = usuario.usu_nombre ?? "";
                        apellido = usuario.usu_apellido ?? "";
                        correo = usuario.usu_correo ?? "";
                    }
                }
            }

            ViewBag.UsuarioId = usuarioId;
            ViewBag.Nombre = nombre;
            ViewBag.Apellido = apellido;
            ViewBag.Correo = correo;

            ViewBag.UsuarioAutenticado = User.Identity.IsAuthenticated;

            // Obtener todas las reservas para mostrar posibles cruces
            ViewBag.TodasLasReservas = await _context.Reserva
                .Include(r => r.Servicio)
                .Select(r => new
                {
                    res_fecha = r.res_fecha,
                    res_hora = r.res_hora,
                    ser_nombre = r.Servicio.ser_nombre
                })
                .ToListAsync();

            return View("Servicio", servicio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int ser_id, string placa, string telefono, string telefono_internacional, DateTime fecha, TimeSpan hora, string detalle)
        {
            var usuario = await ObtenerUsuarioAutenticado();
            if (usuario == null)
                return Unauthorized();

            if (!ValidarFormatoTelefono(telefono_internacional))
            {
                return MostrarErrorReserva(ser_id, "El teléfono debe tener el formato internacional correcto (ej: +51 932454493).");
            }

            var resultadoValidacion = ValidarFechaHora(fecha, hora);
            if (!resultadoValidacion.esValido)
            {
                return MostrarErrorReserva(ser_id, resultadoValidacion.mensaje);
            }

            if (await ExisteReservaParaFecha(usuario.usu_id, fecha))
            {
                return MostrarErrorReserva(ser_id, "Ya tienes una reserva programada para esta fecha. Solo puedes realizar una reserva por día.");
            }

            var reserva = CrearNuevaReserva(ser_id, placa, telefono_internacional, fecha, hora, detalle, usuario.usu_id);

            try
            {
                _context.Reserva.Add(reserva);
                await _context.SaveChangesAsync();
                await EnviarOrdenReservaPorCorreo(reserva);

                TempData["ReservaExitosa"] = true;
                return RedirectToAction("Detalle", new { id = ser_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la reserva");
                return MostrarErrorReserva(ser_id, "Ocurrió un error al procesar tu reserva. Por favor, intenta nuevamente.");
            }
        }

        private async Task<Usuario> ObtenerUsuarioAutenticado()
        {
            if (!User.Identity.IsAuthenticated)
                return null;

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return null;

            return _context.Usuario.FirstOrDefault(u => u.usu_id == identityUser.Id);
        }

        private bool ValidarFormatoTelefono(string telefono)
        {
            return !string.IsNullOrWhiteSpace(telefono) &&
                   System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^\+\d+\s\d{9,15}$");
        }

        private (bool esValido, string mensaje) ValidarFechaHora(DateTime fecha, TimeSpan hora)
        {
            var fechaReserva = fecha.Date + hora;
            var ahora = DateTime.Now;
            var hoy = ahora.Date;

            if (fecha.DayOfWeek == DayOfWeek.Sunday)
                return (false, "No se puede reservar los domingos.");

            if (fechaReserva.Date <= hoy)
                return (false, "Solo puedes reservar para días posteriores al actual.");

            if (fechaReserva <= ahora.AddHours(1))
                return (false, "Solo puedes reservar con al menos 1 hora de anticipación.");

            return (true, string.Empty);
        }

        private async Task<bool> ExisteReservaParaFecha(string usuarioId, DateTime fecha)
        {
            var fechaUtc = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
            return await _context.Reserva.AnyAsync(r =>
                r.UsuarioId == usuarioId &&
                r.res_fecha.Date == fechaUtc.Date);
        }

        private Reserva CrearNuevaReserva(int servicioId, string placa, string telefono,
            DateTime fecha, TimeSpan hora, string detalle, string usuarioId)
        {
            return new Reserva
            {
                res_placa = placa,
                res_telefono = telefono,
                res_fecha = fecha.ToUniversalTime(),
                res_hora = hora,
                UsuarioId = usuarioId,
                ser_id = servicioId,
                res_detalle = detalle
            };
        }

        private IActionResult MostrarErrorReserva(int servicioId, string mensaje)
        {
            TempData["ErrorReserva"] = mensaje;
            return RedirectToAction("Detalle", new { id = servicioId });
        }

        public async Task EnviarOrdenReservaPorCorreo(Reserva reserva)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == reserva.UsuarioId);
            var servicio = await _context.Servicio.FirstOrDefaultAsync(s => s.ser_id == reserva.ser_id);

            if (usuario == null || servicio == null)
                return;

            var user = await _userManager.FindByIdAsync(usuario.usu_id);
            if (user == null)
                return;

            string detalle = string.IsNullOrWhiteSpace(reserva.res_detalle)
                ? "<span style='color:#888;'>Sin detalles adicionales</span>"
                : reserva.res_detalle;

            string emailBody = System.IO.File.ReadAllText("Views/Emails/reserva.html")
                .Replace("{{LogoUrl}}", "https://marimonperu.com/wp-content/uploads/2021/06/logo-web-marimon.png")
                .Replace("{{UserName}}", $"{usuario.usu_nombre} {usuario.usu_apellido}")
                .Replace("{{Servicio}}", servicio.ser_nombre)
                .Replace("{{Fecha}}", reserva.res_fecha.ToString("dd/MM/yyyy"))
                .Replace("{{Hora}}", reserva.res_hora.ToString(@"hh\:mm"))
                .Replace("{{Placa}}", reserva.res_placa)
                .Replace("{{Telefono}}", reserva.res_telefono)
                .Replace("{{Descripcion}}", detalle);

            await _emailSender.SendEmailAsync(user.Email, "Orden de Reserva", emailBody);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarResenia(int ser_id, int res_puntuacion, string res_gusto, string res_comentario)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para agregar una reseña.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }

            var identityUser = await _userManager.GetUserAsync(User);
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == identityUser.Id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }

            var nuevaResenia = new Resenia
            {
                res_puntuacion = res_puntuacion,
                res_gusto = res_gusto,
                res_comentario = res_comentario,
                res_fecha = DateTime.UtcNow,
                UsuarioId = usuario.usu_id,
                ser_id = ser_id
            };

            try
            {
                _context.Resenias.Add(nuevaResenia);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Reseña agregada exitosamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar reseña");
                TempData["Error"] = "Ocurrió un error al agregar la reseña.";
            }

            return RedirectToAction("Detalle", new { id = ser_id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarResenia(int res_id, int ser_id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para eliminar una reseña.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }

            var identityUser = await _userManager.GetUserAsync(User);
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usu_id == identityUser.Id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }

            var resenia = await _context.Resenias.FirstOrDefaultAsync(r => r.res_id == res_id && r.UsuarioId == usuario.usu_id);

            if (resenia == null)
            {
                TempData["Error"] = "Reseña no encontrada o no tienes permiso para eliminarla.";
                return RedirectToAction("Detalle", new { id = ser_id });
            }

            try
            {
                _context.Resenias.Remove(resenia);
                await _context.SaveChangesAsync();
                TempData["ReseniaEliminada"] = "True";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar reseña");
                TempData["Error"] = "Ocurrió un error al eliminar la reseña.";
            }

            return RedirectToAction("Detalle", new { id = ser_id });
        }
    }
}