using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        
        public IActionResult Create()
        {
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "CategoriaId", "Nombre");
            return View();
        }

        // POST: Crear una nueva autoparte
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Autoparte autoparte, IFormFile imagen)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Procesar la imagen si se ha subido
                    if (imagen != null && imagen.Length > 0)
                    {
                        // Ruta donde se almacenará la imagen en wwwroot
                        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "autopartes");

                        // Crear el directorio si no existe
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        // Generar un nombre único para la imagen
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);
                        var filePath = Path.Combine(folderPath, fileName);

                        // Guardar la imagen en la ruta especificada
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imagen.CopyToAsync(stream);
                        }

                        // Guardar la ruta de la imagen en el modelo
                        autoparte.aut_imagen = $"/img/autopartes/{fileName}";
                    }

                    // Agregar la autoparte al contexto de la base de datos
                    _context.Autopartes.Add(autoparte);
                    await _context.SaveChangesAsync();

                    // Mensaje de éxito
                    TempData["SuccessMessage"] = "La autoparte ha sido registrada exitosamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Capturar errores y mostrar un mensaje al usuario
                    TempData["ErrorMessage"] = $"Ocurrió un error: {ex.Message}";
                }
            }

            // Si el modelo no es válido, devolver la vista con los datos del modelo
            ViewBag.Categorias = new SelectList(_context.Categorias, "CategoriaId", "Nombre");
            return View(autoparte);
        }

        // GET: AdminController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
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
            return View();
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
