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
                    OfertaId = a.Ofertas.Max(o => (int?)o.ofe_id),
                    PorcentajeOferta = a.Ofertas.OrderByDescending(o => o.ofe_id).Select(o => (decimal?)o.ofe_porcentaje).FirstOrDefault(),
                    DescripcionOferta = a.Ofertas.OrderByDescending(o => o.ofe_id).Select(o => o.ofe_descripcion).FirstOrDefault(),
                    FechaInicio = a.Ofertas.OrderByDescending(o => o.ofe_id).Select(o => (DateTime?)o.ofe_fecha_inicio).FirstOrDefault(),
                    FechaFin = a.Ofertas.OrderByDescending(o => o.ofe_id).Select(o => (DateTime?)o.ofe_fecha_fin).FirstOrDefault(),
                    PrecioOferta = a.Ofertas.Any()
                        ? a.aut_precio - (a.aut_precio * (a.Ofertas.OrderByDescending(o => o.ofe_id).Select(o => o.ofe_porcentaje).FirstOrDefault() / 100m))
                        : (decimal?)null,
                    OfertaActiva = a.Ofertas.Any()
                        ? (hoy >= a.Ofertas.OrderByDescending(o => o.ofe_id).Select(o => (DateTime?)o.ofe_fecha_inicio).FirstOrDefault()
                        && hoy <= a.Ofertas.OrderByDescending(o => o.ofe_id).Select(o => (DateTime?)o.ofe_fecha_fin).FirstOrDefault())
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
                return await EditarOferta(productosSeleccionados, ofe_descripcion, ofe_porcentaje, ofe_fecha_inicio, ofe_fecha_fin);
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
                var ids = productosSeleccionados.ToList();
                var autopartesExistentes = await _context.Autopartes
                    .Where(a => ids.Contains(a.aut_id))
                    .Select(a => a.aut_id)
                    .ToListAsync();

                var ofertasExistentes = await _context.Ofertas
                    .Where(o => ids.Contains(o.AutoparteId))
                    .ToListAsync();

                _context.Ofertas.RemoveRange(ofertasExistentes);

                var nuevasOfertas = autopartesExistentes.Select(autoparteId => new Oferta
                {
                    AutoparteId = autoparteId,
                    ofe_descripcion = descripcion,
                    ofe_porcentaje = porcentaje,
                    ofe_fecha_inicio = fechaInicio,
                    ofe_fecha_fin = fechaFin,
                    ofe_activa = true
                });

                _context.Ofertas.AddRange(nuevasOfertas);

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Se aplicó la oferta correctamente a {autopartesExistentes.Count} producto(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar ofertas");
                TempData["Error"] = "Ocurrió un error al aplicar las ofertas. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Metido Edita la oferta de los productos seleccionados
        private async Task<IActionResult> EditarOferta(int[] productosSeleccionados, 
            string descripcion, decimal porcentaje, DateTime fechaInicio, DateTime fechaFin)
        {
            if (productosSeleccionados == null || productosSeleccionados.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para editar.";
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
                // Buscar ofertas existentes para los productos seleccionados
                var ofertasExistentes = await _context.Ofertas
                    .Where(o => productosSeleccionados.Contains(o.AutoparteId))
                    .ToListAsync();

                if (ofertasExistentes.Count == 0)
                {
                    TempData["Error"] = "Los productos seleccionados no tienen ofertas para editar.";
                    return RedirectToAction(nameof(Index));
                }

                // Determinar si la oferta está activa basada en la fecha actual
                var hoy = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
                bool ofertaActiva = (hoy >= fechaInicio && hoy <= fechaFin);

                // Actualizar cada oferta existente
                foreach (var oferta in ofertasExistentes)
                {
                    oferta.ofe_descripcion = descripcion;
                    oferta.ofe_porcentaje = porcentaje;
                    oferta.ofe_fecha_inicio = fechaInicio;
                    oferta.ofe_fecha_fin = fechaFin;
                    oferta.ofe_activa = ofertaActiva;
                }

                _context.UpdateRange(ofertasExistentes);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Se actualizaron {ofertasExistentes.Count} oferta(s) correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar ofertas");
                TempData["Error"] = "Ocurrió un error al editar las ofertas. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        //método EliminarOferta actual con este:
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