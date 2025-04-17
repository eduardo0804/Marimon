using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Marimon.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: AdminController
        public ActionResult Index()
        {
            return View();
        }

        // GET: AdminController/Details/5
        public ActionResult Details(int id)
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

                _logger.LogInformation($"Productos encontrados para categoría {categoriaId}: {productos.Count}");
                return Json(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener productos por categoría: {ex.Message}");
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
                    aut_precio = autoparte.aut_precio, // Guardar el precio actual
                    ent_fechaent = DateOnly.FromDateTime(DateTime.Now),
                    aut_cantidad = autoparte.aut_cantidad + ent_cantidad // Actualizar la cantidad total
                };

                // Actualizar el stock de la autoparte
                autoparte.aut_cantidad += ent_cantidad;

                // Guardar los cambios en la base de datos
                _context.Entradas.Add(entrada);
                _context.Autopartes.Update(autoparte);
                _context.SaveChanges();

                _logger.LogInformation($"Entrada registrada: Producto ID {AutoparteId}, Cantidad {ent_cantidad}");
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
            Console.WriteLine("=== Iniciando registro de autoparte ===");

            // Log de los datos del modelo que vienen desde el formulario
            Console.WriteLine($"aut_nombre: {autoparte.aut_nombre}");
            Console.WriteLine($"aut_descripcion: {autoparte.aut_descripcion}");
            Console.WriteLine($"aut_precio: {autoparte.aut_precio}");
            Console.WriteLine($"CategoriaId: {autoparte.CategoriaId}");

            // Log del archivo recibido
            Console.WriteLine($"Imagen: {imagen?.FileName}, Tamaño: {imagen?.Length}");

            // Verificar si ya existe una autoparte con el mismo nombre
            var autoparteExistente = await _context.Autopartes
            .FirstOrDefaultAsync(a => a.aut_nombre == autoparte.aut_nombre);

            // Verificar que el archivo haya sido enviado y tenga contenido
            if (imagen == null || imagen.Length == 0)
            {
                Console.WriteLine("=> No se recibió imagen.");
                ModelState.AddModelError("imagen", "La imagen es obligatoria.");
            }

            // Aquí imprimimos el estado del ModelState
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=> ModelState NO es válido. Errores:");
                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        Console.WriteLine($"Error en campo '{error.Key}': {subError.ErrorMessage}");
                    }
                }
                ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
                return View(autoparte);
            }

            try
            {
                // Procesar la imagen: asigna un nombre y guarda en wwwroot/uploads
                var nombreArchivo = Path.GetFileName(imagen.FileName);
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Console.WriteLine($"Ruta de carpeta de uploads: {rutaCarpeta}");

                if (!Directory.Exists(rutaCarpeta))
                {
                    Console.WriteLine("=> Carpeta no existe, creando carpeta...");
                    Directory.CreateDirectory(rutaCarpeta);
                }

                var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);
                Console.WriteLine($"Ruta completa del archivo: {rutaCompleta}");

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    Console.WriteLine("=> Guardando imagen en disco...");
                    await imagen.CopyToAsync(stream);
                }

                // Asignar la ruta relativa de la imagen al modelo
                autoparte.aut_imagen = "/uploads/" + nombreArchivo;
                Console.WriteLine($"=> Ruta de imagen asignada a autoparte: {autoparte.aut_imagen}");

                // Log de todos los datos del modelo antes de guardarlo
                Console.WriteLine("=== Datos finales del modelo antes de guardar ===");
                Console.WriteLine($"aut_nombre: {autoparte.aut_nombre}");
                Console.WriteLine($"aut_descripcion: {autoparte.aut_descripcion}");
                Console.WriteLine($"aut_precio: {autoparte.aut_precio}");
                Console.WriteLine($"aut_especificacion: {autoparte.aut_especificacion}");
                Console.WriteLine($"aut_imagen: {autoparte.aut_imagen}");
                Console.WriteLine($"CategoriaId: {autoparte.CategoriaId}");

                // Guardar el modelo en la base de datos
                _context.Autopartes.Add(autoparte);
                await _context.SaveChangesAsync();
                Console.WriteLine("=> Autoparte guardada exitosamente en la base de datos.");

                // Usamos TempData para pasar el mensaje de éxito
                TempData["SuccessMessage"] = "La autoparte se registró correctamente.";

                // CORRECCIÓN: Usamos return JavaScript para cerrar el modal y recargar la página
                // para que los toasts se muestren correctamente
                return Content("<script>window.parent.location.href = '/Admin/ListaAutopartes';</script>", "text/html");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Create: {ex.Message}");
                ModelState.AddModelError("", $"Error al registrar la autoparte: {ex.Message}");
                _logger.LogError($"Error en Create: {ex}");

                ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");
                return View(autoparte);
            }
        }
        [HttpGet]
        public IActionResult Editar(int id)
        {
            var autoparte = _context.Autopartes.Find(id);
            if (autoparte == null)
            {
                return NotFound();
            }

            // Llenar el ViewBag con las categorías para el dropdown
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "cat_id", "cat_nombre");

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
                var fileName = Path.GetFileNameWithoutExtension(imagen.FileName);
                var fileExtension = Path.GetExtension(imagen.FileName);
                var uniqueFileName = fileName + "_" + Guid.NewGuid().ToString() + fileExtension;

                var imagePath = Path.Combine(_hostingEnvironment.WebRootPath, "images", uniqueFileName);

                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }

                autoparte.aut_imagen = "/images/" + uniqueFileName;
            }
            else
            {
                // Mantener la imagen anterior si no se sube una nueva
                autoparte.aut_imagen = autoparteExistente.aut_imagen;
            }

            _context.Autopartes.Update(autoparte);
            await _context.SaveChangesAsync();

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

            return RedirectToAction("ListaAutopartes", "Admin");
        }
    }
}