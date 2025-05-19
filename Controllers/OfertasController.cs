using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Marimon.Data;
using Marimon.Models;
using Marimon.Models.ViewModels;
using System.Globalization;

namespace Marimon.Controllers
{
    public class OfertasController : Controller
    {
        private readonly ILogger<OfertasController> _logger;
        private readonly ApplicationDbContext _context;

        public OfertasController(ILogger<OfertasController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

            var autopartes = await _context.Autopartes
                .Include(a => a.Categoria)
                .Include(a => a.Ofertas)
                .Where(a => a.aut_cantidad >= 1)
                .OrderBy(a => a.aut_id)
                .Select(a => new AutoparteViewModel
                {
                    aut_id = a.aut_id,
                    aut_nombre = a.aut_nombre,
                    aut_precio = a.aut_precio,
                    aut_cantidad = a.aut_cantidad,
                    aut_imagen = a.aut_imagen,
                    CategoriaNombre = a.Categoria.cat_nombre,
                    OfertaId = a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_id,
                    PorcentajeOferta = a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_porcentaje,
                    DescripcionOferta = a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_descripcion,
                    FechaInicio = a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_fecha_inicio,
                    FechaFin = a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_fecha_fin,
                    PrecioOferta = a.Ofertas.Any() 
                        ? a.aut_precio - (a.aut_precio * (a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_porcentaje / 100m))
                        : (decimal?)null,
                    OfertaActiva = a.Ofertas.Any() 
                        ? (hoy >= a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_fecha_inicio 
                        && hoy <= a.Ofertas.OrderByDescending(o => o.ofe_id).FirstOrDefault().ofe_fecha_fin)
                        : (bool?)null
                })
                .ToListAsync();

            int totalProductosEnStock = autopartes.Count;

            var viewModel = new OfertasViewModel
            {
                Autopartes = autopartes,
                TotalProductosEnStock = totalProductosEnStock
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Index(string action, int[] productosSeleccionados, 
            string ofe_descripcion, decimal ofe_porcentaje, DateTime ofe_fecha_inicio, DateTime ofe_fecha_fin)
        {
            if (action == "aplicar")
            {
                return await AplicarOferta(productosSeleccionados, ofe_descripcion, ofe_porcentaje, ofe_fecha_inicio, ofe_fecha_fin);
            }
            else if (action == "editar")
            {
                return await EditarOferta(productosSeleccionados);
            }
            else if (action == "eliminar")
            {
                return await EliminarOferta(productosSeleccionados);
            }

            // Si ninguna acción válida, redirigir al GET
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> AplicarOferta(int[] productosSeleccionados, 
            string descripcion, decimal porcentaje, DateTime fechaInicio, DateTime fechaFin)
        {
            if (productosSeleccionados == null || productosSeleccionados.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para aplicar la oferta.";
                return RedirectToAction(nameof(Index));
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                TempData["Error"] = "La descripción de la oferta es obligatoria.";
                return RedirectToAction(nameof(Index));
            }

            if (porcentaje <= 0 || porcentaje >= 100)
            {
                TempData["Error"] = "El porcentaje debe ser entre 1 y 99.";
                return RedirectToAction(nameof(Index));
            }

            if (fechaInicio >= fechaFin)
            {
                TempData["Error"] = "La fecha de inicio debe ser anterior a la fecha de fin.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Crear ofertas para cada producto seleccionado
                foreach (var autoparteId in productosSeleccionados)
                {
                    // Verificar si el producto existe
                    var autoparte = await _context.Autopartes.FindAsync(autoparteId);
                    if (autoparte == null) continue;

                    // Verificar si ya tiene una oferta activa y eliminarla
                    var ofertaExistente = await _context.Ofertas
                        .Where(o => o.AutoparteId == autoparteId)
                        .FirstOrDefaultAsync();

                    if (ofertaExistente != null)
                    {
                        _context.Ofertas.Remove(ofertaExistente);
                    }

                    // Crear nueva oferta
                    var nuevaOferta = new Oferta
                    {
                        AutoparteId = autoparteId,
                        ofe_descripcion = descripcion,
                        ofe_porcentaje = porcentaje,
                        ofe_fecha_inicio = fechaInicio,
                        ofe_fecha_fin = fechaFin,
                        ofe_activa = true
                    };

                    _context.Ofertas.Add(nuevaOferta);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Se aplicó la oferta correctamente a {productosSeleccionados.Length} producto(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar ofertas");
                TempData["Error"] = "Ocurrió un error al aplicar las ofertas. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Reemplaza el método EditarOferta actual con este:
        private async Task<IActionResult> EditarOferta(int[] productosSeleccionados)
        {
            if (productosSeleccionados == null || productosSeleccionados.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para editar.";
                return RedirectToAction(nameof(Index));
            }

            if (productosSeleccionados.Length > 1)
            {
                TempData["Error"] = "Solo puede editar una oferta a la vez. Seleccione un solo producto.";
                return RedirectToAction(nameof(Index));
            }

            var autoparteId = productosSeleccionados[0];
            var oferta = await _context.Ofertas
                .Include(o => o.Autoparte)
                .Where(o => o.AutoparteId == autoparteId)
                .FirstOrDefaultAsync();

            if (oferta == null)
            {
                TempData["Error"] = "El producto seleccionado no tiene ninguna oferta para editar.";
                return RedirectToAction(nameof(Index));
            }

            // Redirigir a una vista de edición específica con el ID de la oferta
            return RedirectToAction(nameof(EditarOfertaForm), new { id = oferta.ofe_id });
        }

        // Agregar estos nuevos métodos:
        public async Task<IActionResult> EditarOfertaForm(int id)
        {
            var oferta = await _context.Ofertas
                .Include(o => o.Autoparte)
                .FirstOrDefaultAsync(o => o.ofe_id == id);

            if (oferta == null)
            {
                return NotFound();
            }

            return View(oferta);
        }

        [HttpPost]
        public async Task<IActionResult> EditarOfertaForm(int id, string ofe_descripcion, decimal ofe_porcentaje, 
            DateTime ofe_fecha_inicio, DateTime ofe_fecha_fin)
        {
            var oferta = await _context.Ofertas.FindAsync(id);

            if (oferta == null)
            {
                return NotFound();
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(ofe_descripcion))
            {
                ModelState.AddModelError("ofe_descripcion", "La descripción de la oferta es obligatoria.");
                return View(oferta);
            }

            if (ofe_porcentaje <= 0 || ofe_porcentaje >= 100)
            {
                ModelState.AddModelError("ofe_porcentaje", "El porcentaje debe ser entre 1 y 99.");
                return View(oferta);
            }

            if (ofe_fecha_inicio >= ofe_fecha_fin)
            {
                ModelState.AddModelError("ofe_fecha_inicio", "La fecha de inicio debe ser anterior a la fecha de fin.");
                return View(oferta);
            }

            try
            {
                // Actualizar los campos de la oferta
                oferta.ofe_descripcion = ofe_descripcion;
                oferta.ofe_porcentaje = ofe_porcentaje;
                oferta.ofe_fecha_inicio = ofe_fecha_inicio;
                oferta.ofe_fecha_fin = ofe_fecha_fin;
                
                // Determinar si la oferta está activa basada en la fecha actual
                var hoy = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
                oferta.ofe_activa = (hoy >= ofe_fecha_inicio && hoy <= ofe_fecha_fin);

                _context.Update(oferta);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "La oferta ha sido actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la oferta");
                ModelState.AddModelError("", "Ocurrió un error al actualizar la oferta. Intente nuevamente.");
                return View(oferta);
            }
        }

        // Reemplaza el método EliminarOferta actual con este:
        private async Task<IActionResult> EliminarOferta(int[] productosSeleccionados)
        {
            if (productosSeleccionados == null || productosSeleccionados.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para eliminar ofertas.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var ofertasAEliminar = await _context.Ofertas
                    .Where(o => productosSeleccionados.Contains(o.AutoparteId))
                    .ToListAsync();

                if (ofertasAEliminar.Count == 0)
                {
                    TempData["Error"] = "Los productos seleccionados no tienen ofertas para eliminar.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Ofertas.RemoveRange(ofertasAEliminar);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Se eliminaron {ofertasAEliminar.Count} oferta(s) correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar ofertas");
                TempData["Error"] = "Ocurrió un error al eliminar las ofertas. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}