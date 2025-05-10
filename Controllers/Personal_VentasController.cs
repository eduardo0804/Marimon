using Google.Apis.Auth.OAuth2;
using Marimon.Data;
using Marimon.Models;
using Marimon.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Marimon.Controllers
{
    public class Personal_VentasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<Personal_VentasController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailSenderWithAttachments _emailSender;

        public Personal_VentasController(ApplicationDbContext context, ILogger<Personal_VentasController> logger, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Personal_VentasController
        public ActionResult Index()
        {
            return View();
        }

        // GET: Admin/Entradas - Página de entrada de productos
        public ActionResult Entradas()
        {
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");

            // Cargar la lista de entradas para mostrar en la tabla
            var listaEntradas = _context.Entradas
                .Include(e => e.Autoparte)
                .OrderByDescending(e => e.ent_id)
                .Take(10)  // Limitar a las últimas 20 entradas
                .ToList();

            ViewBag.ListaEntradas = listaEntradas;

            return View("~/Views/Flujos/Entradas.cshtml");
        }

        // AJAX - Obtener productos por categoría
        [HttpGet]
        public IActionResult ObtenerProductosPorCategoria(int categoriaId)
        {
            try
            {
                // Usar una clase auxiliar en lugar de un tipo anónimo
                var productos = _context.Autopartes
                    .Where(p => p.CategoriaId == categoriaId)
                    .Select(p => new ProductoDTO
                    {
                        aut_id = p.aut_id,
                        aut_nombre = p.aut_nombre ?? string.Empty
                    })
                    .ToList();

                return Json(productos);
            }
            catch (Exception ex)
            {
                var errorResponse = new Dictionary<string, string> { { "error", "Error al obtener productos" } };
                return Json(errorResponse);
            }
        }

        // Clase DTO para evitar tipos anónimos
        public class ProductoDTO
        {
            public int aut_id { get; set; }
            public string aut_nombre { get; set; } = string.Empty;
        }

        // POST: Admin/RegistrarEntrada - Procesar entrada de productos
        [HttpPost]
        public IActionResult RegistrarEntrada(int AutoparteId, int ent_cantidad, string ent_proveedor = "")
        {

            try
            {
                // Validar entradas
                if (AutoparteId <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un producto válido.";
                    return RedirectToAction("Entradas");
                }

                if (ent_cantidad <= 0)
                {
                    TempData["Error"] = "La cantidad debe ser mayor a 0.";
                    return RedirectToAction("Entradas");
                }

                // Buscar la autoparte correspondiente
                var autoparte = _context.Autopartes.Find(AutoparteId);
                if (autoparte == null)
                {
                    TempData["Error"] = "No se encontró el producto seleccionado.";
                    return RedirectToAction("Entradas");
                }

                // Crear el registro de entrada
                var entrada = new Entradas
                {
                    AutoparteId = AutoparteId,
                    ent_cantidad = ent_cantidad,
                    ent_proveedor = ent_proveedor,
                    ent_fechaent = DateOnly.FromDateTime(DateTime.Now),
                };

                autoparte.aut_cantidad += ent_cantidad;

                _context.Entradas.Add(entrada);
                _context.Autopartes.Update(autoparte);
                _context.SaveChanges();

                TempData["Mensaje"] = $"Se han registrado {ent_cantidad} unidades de {autoparte.aut_nombre} correctamente.";
                return RedirectToAction("Entradas");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar entrada: {ex.Message}");
                TempData["Error"] = "Error al registrar la entrada: " + ex.Message;
                return RedirectToAction("Entradas");
            }
        }

        // Fix for CS8602: Desreferencia de una referencia posiblemente NULL.
        public ActionResult Salidas()
        {
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
            // Agrupar las salidas por VentaId
            var salidasAgrupadas = _context.Salida
                .Include(s => s.Autoparte)
                .Include(s => s.Comprobante)
                    .ThenInclude(c => c.Venta)
                .Where(s => s.Comprobante != null && s.Comprobante.Venta != null)
                .OrderByDescending(s => s.sal_id)
                .GroupBy(s => s.Comprobante!.Venta!.ven_id)
                .Select(g => new
                {
                    VentaId = g.Key,
                    Estado = g.FirstOrDefault()!.Comprobante!.Venta!.Estado, // Incluir el estado de la venta
                    Salidas = g.ToList()
                })
                .ToList();
            ViewBag.SalidasAgrupadas = salidasAgrupadas;
            return View("~/Views/Flujos/Salida.cshtml");
        }




        // POST: Admin/RegistrarSalida - Procesar salida de productos
        [HttpPost]
        public async Task<IActionResult> RegistrarSalida(int AutoparteId, int sal_cantidad)
        {
            try
            {
                // Validar entradas
                if (AutoparteId <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un producto válido.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }

                if (sal_cantidad <= 0)
                {
                    TempData["Error"] = "La cantidad debe ser mayor a 0.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }

                // Buscar la autoparte correspondiente
                var autoparte = await _context.Autopartes.FindAsync(AutoparteId);
                if (autoparte == null)
                {
                    TempData["Error"] = "No se encontró el producto seleccionado.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }
                else if (autoparte.aut_cantidad < sal_cantidad)
                {
                    TempData["Error"] = "No hay suficiente inventario para realizar la salida.";
                    return RedirectToAction("Salidas");  // FIXED: Changed to "Salidas"
                }

                // Validar stock disponible
                if (autoparte.aut_cantidad < sal_cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente. Solo hay {autoparte.aut_cantidad} unidades disponibles.";
                    return RedirectToAction("Salidas");
                }

                // Paso 6: Registrar la salida
                var salida = new Salida
                {
                    AutoparteId = AutoparteId,
                    sal_cantidad = sal_cantidad,
                    sal_fechasalida = DateOnly.FromDateTime(DateTime.Now),
                    // ComprobanteId = comprobante.com_id // Relacionar con el comprobante ya guardado
                };
                _context.Salida.Add(salida);

                // ADDED: Update inventory quantity
                autoparte.aut_cantidad -= sal_cantidad;
                _context.Autopartes.Update(autoparte);

                await _context.SaveChangesAsync(); // Guardar todo
                TempData["ShowModal"] = true;
                TempData["ProductoNombre"] = autoparte.aut_nombre;
                TempData["Cantidad"] = sal_cantidad;

                TempData["Mensaje"] = $"Se han registrado la salida de {sal_cantidad} unidades de {autoparte.aut_nombre} correctamente.";
                return RedirectToAction("Salidas");  // FIXED: Changed to redirect to Salidas instead of returning Ok
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar salida: {ex.Message}");
                TempData["Error"] = "Error al registrar la salida: " + ex.Message;  // FIXED: Changed "entrada" to "salida"
                return RedirectToAction("Salidas");
            }
        }

        [HttpPost]
        public IActionResult CambiarEstadoAvanzado(int id, string estado)
        {
            try
            {
                // Buscar la venta por ID
                var venta = _context.Venta.FirstOrDefault(v => v.ven_id == id);
                if (venta == null)
                {
                    TempData["Error"] = "Venta no encontrada.";
                    return RedirectToAction("Salidas");
                }

                // Validar el estado
                if (estado != "Completado" && estado != "Pendiente" && estado != "Cancelado")
                {
                    TempData["Error"] = "Estado no válido.";
                    return RedirectToAction("Salidas");
                }

                string estadoAnterior = venta.Estado;
                if (estadoAnterior == estado)
                {
                    TempData["Mensaje"] = $"La venta #{id} ya se encuentra en estado {estado}.";
                    return RedirectToAction("Salidas");
                }

                // Actualizar el estado
                venta.Estado = estado;
                _context.SaveChanges();

                TempData["Mensaje"] = $"El estado de la venta #{id} ha sido actualizado de {estadoAnterior} a {estado}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar el estado: {ex.Message}";
            }

            return RedirectToAction("Salidas");
        }





        //AUTOPARTE - CRUD

        // GET: Admin/ListaAutopartes
        public async Task<IActionResult> ListaAutopartes()
        {
            var autopartes = await _context.Autopartes
                .Include(a => a.Categoria)  // Cargar la categoría relacionada
                .OrderBy(a => a.aut_id) // Aquí defines el orden
                .ToListAsync();

            return View(autopartes);
        }
        public IActionResult Create()
        {
            ViewBag.Title = "Registrar Autoparte";
            ViewBag.ButtonText = "Registrar";
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
            return PartialView("Create"); // Asegúrate de que sea una vista parcial
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Autoparte autoparte, IFormFile imagen)
        {
            try
            {
                var bucket = "marimonapp.appspot.com";
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);

                // Ruta variable de entorno para las credenciales de Firebase
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
                        var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name=autopartes/{uniqueFileName}";
                        var content = new StreamContent(stream);
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                        var response = await httpClient.PostAsync(url, content);
                        response.EnsureSuccessStatusCode();

                        // URL pública de la imagen
                        var publicUrl = $"https://storage.googleapis.com/{bucket}/autopartes/{uniqueFileName}";
                        autoparte.aut_imagen = publicUrl;
                    }
                }
                _context.Autopartes.Add(autoparte);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "La autoparte se registró correctamente.";

                return Content("<script>window.parent.location.href = '/Admin/ListaAutopartes';</script>", "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar la autoparte");
                ModelState.AddModelError("", $"Error al registrar la autoparte: {ex.Message}");
                ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
                return View(autoparte);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var autoparte = await _context.Autopartes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.aut_id == id);

            if (autoparte == null)
            {
                return NotFound();
            }

            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre", autoparte.CategoriaId);
            return PartialView("_EditarAutoparte", autoparte);
        }
        [HttpPost]
        public async Task<IActionResult> Editar(Autoparte autoparte, IFormFile imagen)
        {
            // Recuperar la autoparte existente desde la base de datos
            var autoparteExistente = await _context.Autopartes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.aut_id == autoparte.aut_id);

            if (autoparteExistente == null)
            {
                return NotFound();
            }

            if (imagen != null && imagen.Length > 0)
            {
                // Subir la nueva imagen a Firebase Storage
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
                        var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name=autopartes/{uniqueFileName}";
                        var content = new StreamContent(stream);
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imagen.ContentType);

                        var response = await httpClient.PostAsync(url, content);
                        response.EnsureSuccessStatusCode();

                        var publicUrl = $"https://storage.googleapis.com/{bucket}/autopartes/{uniqueFileName}";
                        autoparte.aut_imagen = publicUrl;
                    }

                    // Eliminar la imagen anterior de Firebase Storage si existe
                    if (!string.IsNullOrEmpty(autoparteExistente.aut_imagen))
                    {
                        var oldImageName = autoparteExistente.aut_imagen.Split('/').Last();
                        var deleteUrl = $"https://storage.googleapis.com/storage/v1/b/{bucket}/o/autopartes%2F{oldImageName}";

                        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                        deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                        var deleteResponse = await httpClient.SendAsync(deleteRequest);
                        if (!deleteResponse.IsSuccessStatusCode)
                        {
                            _logger.LogWarning($"No se pudo eliminar la imagen anterior: {autoparteExistente.aut_imagen}");
                        }
                    }
                }
            }
            else
            {
                // Mantener la imagen anterior si no se sube una nueva
                autoparte.aut_imagen = autoparteExistente.aut_imagen;
            }

            // Actualizar los datos de la autoparte
            _context.Autopartes.Update(autoparte);
            await _context.SaveChangesAsync();

            TempData["EditMessage"] = "La autoparte se actualizó correctamente.";
            return RedirectToAction("ListaAutopartes", "Admin");
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var autoparte = await _context.Autopartes.FindAsync(id);

            if (autoparte == null)
            {
                return NotFound();
            }

            // Eliminar la imagen del servidor si existe
            if (!string.IsNullOrEmpty(autoparte.aut_imagen))
            {
                var rutaImagen = Path.Combine(_hostingEnvironment.WebRootPath, autoparte.aut_imagen.TrimStart('/'));

                if (System.IO.File.Exists(rutaImagen))
                {
                    System.IO.File.Delete(rutaImagen);
                }
            }

            // Eliminar la autoparte de la base de datos
            _context.Autopartes.Remove(autoparte);
            await _context.SaveChangesAsync();
            TempData["DeleteMessage"] = "La autoparte se eliminó correctamente.";
            return RedirectToAction("ListaAutopartes", "Admin");
        }
    }
}