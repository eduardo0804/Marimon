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

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
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
        
        // GET: Admin/ListaAutopartes
        public async Task<IActionResult> ListaAutopartes()
        {
            var autopartes = await _context.Autopartes
                                        .Include(a => a.Categoria)
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

                return RedirectToAction("ListaAutopartes", "Admin");
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


        // GET: AdminController/Edit/5
        public ActionResult Edit(int id)
        {
            var autoparte = _context.Autopartes.Find(id);
            if (autoparte == null) return NotFound();
            return View(autoparte);
        }

        // POST: AdminController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdminController/Delete/5
        public ActionResult Delete(int id)
        {
            var autoparte = _context.Autopartes.Find(id);
            if (autoparte == null) return NotFound();
            return View(autoparte);
        }

        // POST: AdminController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        
    }
}
