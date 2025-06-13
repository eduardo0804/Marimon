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
using System.IO;
using Marimon.Services;

namespace Marimon.Controllers
{
    public class DescuentosController : Controller
    {
        private readonly ILogger<DescuentosController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSenderWithAttachments _emailSender;

        public DescuentosController(ILogger<DescuentosController> logger, ApplicationDbContext context, IEmailSenderWithAttachments emailSender)
        {
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

            var autopartesDb = await _context.Autopartes
                .Include(a => a.Categoria)
                .Include(a => a.CodigosDescuento)
                .Where(a => a.aut_cantidad >= 1)
                .OrderBy(a => a.aut_id)
                .ToListAsync();

            var autopartes = autopartesDb.Select(a => {
                var codigos = a.CodigosDescuento ?? new List<CodigoDescuento>();
                var ultimoDescuento = codigos.Any() ? codigos.OrderByDescending(c => c.cod_id).First() : null;
                return new AutoparteViewModel
                {
                    aut_id = a.aut_id,
                    aut_nombre = a.aut_nombre ?? string.Empty,
                    aut_precio = a.aut_precio,
                    aut_cantidad = a.aut_cantidad,
                    aut_imagen = a.aut_imagen ?? string.Empty,
                    CategoriaNombre = a.Categoria?.cat_nombre ?? string.Empty,
                    CodigoDescuentoId = ultimoDescuento != null ? ultimoDescuento.cod_id : 0,
                    CodigoDescuento = ultimoDescuento != null ? ultimoDescuento.cod_codigo : null,
                    PorcentajeDescuento = ultimoDescuento != null ? ultimoDescuento.cod_porcentaje : (decimal?)null,
                    DescripcionDescuento = ultimoDescuento != null ? ultimoDescuento.cod_descripcion : null,
                    FechaInicioDescuento = ultimoDescuento != null ? ultimoDescuento.cod_fecha_creacion : (DateTime?)null,
                    FechaFinDescuento = ultimoDescuento != null ? ultimoDescuento.cod_fecha_expiracion : (DateTime?)null,
                    PrecioDescuento = ultimoDescuento != null 
                        ? a.aut_precio - (a.aut_precio * (ultimoDescuento.cod_porcentaje / 100m))
                        : (decimal?)null,
                    DescuentoActivo = ultimoDescuento != null 
                        ? (hoy >= ultimoDescuento.cod_fecha_creacion && hoy <= ultimoDescuento.cod_fecha_expiracion)
                        : (bool?)null,
                    TieneDescuento = codigos.Any()
                };
            }).ToList();

            int totalProductosEnStock = autopartes.Count;

            var viewModel = new DescuentosViewModel
            {
                Autopartes = autopartes,
                TotalProductosEnStock = totalProductosEnStock
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(string action, int[] productosSeleccionados, 
            string cod_codigo, string cod_descripcion, decimal cod_porcentaje, 
            DateTime cod_fecha_creacion, DateTime cod_fecha_expiracion)
        {
            // Verificar que realmente sea una acción POST válida
            if (string.IsNullOrEmpty(action))
            {
                return RedirectToAction(nameof(Index));
            }

            if (action == "aplicar")
            {
                return await AplicarDescuento(productosSeleccionados, cod_codigo, cod_descripcion, 
                    cod_porcentaje, cod_fecha_creacion, cod_fecha_expiracion);
            }
            else if (action == "eliminar")
            {
                return await EliminarDescuento(productosSeleccionados);
            }
            else if (action == "enviar")
            {
                return await EnviarCodigoDescuento(productosSeleccionados);
            }

            // Si ninguna acción válida, redirigir al GET
            return RedirectToAction(nameof(Index));
        }
        
        private async Task<IActionResult> AplicarDescuento(int[] productosSeleccionados,
            string codigo, string descripcion, decimal porcentaje, DateTime fechaInicio, DateTime fechaFin)
        {
            if (productosSeleccionados == null || productosSeleccionados.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para aplicar el código de descuento.";
                return RedirectToAction(nameof(Index));
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Error"] = "El código de descuento es obligatorio.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                TempData["Error"] = "La descripción del código de descuento es obligatoria.";
                return RedirectToAction(nameof(Index));
            }

            if (porcentaje <= 0 || porcentaje >= 100)
            {
                TempData["Error"] = "El porcentaje debe ser entre 1 y 99.";
                return RedirectToAction(nameof(Index));
            }

            if (fechaInicio >= fechaFin)
            {
                TempData["Error"] = "La fecha de inicio debe ser anterior a la fecha de expiración.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar que el código no exista ya
            var codigoExistente = await _context.CodigosDescuento
                .AnyAsync(c => c.cod_codigo == codigo);

            if (codigoExistente)
            {
                TempData["Error"] = "Ya existe un código de descuento con ese nombre.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Crear códigos de descuento para cada producto seleccionado
                foreach (var autoparteId in productosSeleccionados)
                {
                    // Verificar si el producto existe
                    var autoparte = await _context.Autopartes.FindAsync(autoparteId);
                    if (autoparte == null) continue;

                    // Verificar si ya tiene un código de descuento activo y eliminarlo
                    var descuentoExistente = await _context.CodigosDescuento
                        .Where(c => c.AutoparteId == autoparteId)
                        .FirstOrDefaultAsync();

                    if (descuentoExistente != null)
                    {
                        _context.CodigosDescuento.Remove(descuentoExistente);
                    }

                    // Crear nuevo código de descuento
                    var nuevoDescuento = new CodigoDescuento
                    {
                        AutoparteId = autoparteId,
                        cod_codigo = codigo,
                        cod_descripcion = descripcion,
                        cod_porcentaje = porcentaje,
                        cod_fecha_creacion = fechaInicio,
                        cod_fecha_expiracion = fechaFin,
                        cod_utilizado = false,
                        UsuarioId = null // Se asignará cuando se envíe a usuarios específicos
                    };

                    _context.CodigosDescuento.Add(nuevoDescuento);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Se aplicó el código de descuento correctamente a {productosSeleccionados.Length} producto(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar códigos de descuento");
                TempData["Error"] = "Ocurrió un error al aplicar los códigos de descuento. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }


        //Funcion Elimina todos los productos selecionados en la tabla
        private async Task<IActionResult> EliminarDescuento(int[] productosSeleccionados)
        {
            if (productosSeleccionados == null || productosSeleccionados.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para eliminar códigos de descuento.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var descuentosAEliminar = await _context.CodigosDescuento
                    .Where(c => productosSeleccionados.Contains(c.AutoparteId.Value))
                    .ToListAsync();

                if (descuentosAEliminar.Count == 0)
                {
                    TempData["Error"] = "Los productos seleccionados no tienen códigos de descuento para eliminar.";
                    return RedirectToAction(nameof(Index));
                }

                _context.CodigosDescuento.RemoveRange(descuentosAEliminar);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Se eliminaron {descuentosAEliminar.Count} código(s) de descuento correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar códigos de descuento");
                TempData["Error"] = "Ocurrió un error al eliminar los códigos de descuento. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> EnviarCodigoDescuento(int[] productosSeleccionados)
        {
            if (productosSeleccionados == null || productosSeleccionados.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para enviar códigos de descuento.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Obtener usuarios con más de 10 ventas
                var usuariosConMasDe10Ventas = await _context.Usuarios
                    .Where(u => u.Ventas.Count() >= 1)
                    .ToListAsync();

                if (usuariosConMasDe10Ventas.Count == 0)
                {
                    TempData["Warning"] = "No se encontraron usuarios con más de 10 ventas para enviar los códigos.";
                    return RedirectToAction(nameof(Index));
                }

                int codigosCreados = 0;
                int emailsEnviados = 0;

                foreach (var autoparteId in productosSeleccionados)
                {
                    // Obtener plantilla descuento para ese producto (sin usuario)
                    var plantillaDescuento = await _context.CodigosDescuento
                        .Include(c => c.Autoparte)
                        .FirstOrDefaultAsync(c => c.AutoparteId == autoparteId && c.UsuarioId == null);

                    if (plantillaDescuento == null)
                        continue; // No hay descuento plantilla para ese producto, sigue al siguiente

                    foreach (var usuario in usuariosConMasDe10Ventas)
                    {
                        // Verificar si el usuario ya tiene código para este producto
                        var yaExiste = await _context.CodigosDescuento
                            .AnyAsync(c => c.UsuarioId == usuario.usu_id &&
                                        c.AutoparteId == autoparteId &&
                                        c.cod_codigo == plantillaDescuento.cod_codigo);

                        if (!yaExiste)
                        {
                            var codigoUnico = new CodigoDescuento
                            {
                                AutoparteId = autoparteId,
                                cod_codigo = plantillaDescuento.cod_codigo,
                                cod_descripcion = plantillaDescuento.cod_descripcion,
                                cod_porcentaje = plantillaDescuento.cod_porcentaje,
                                cod_fecha_creacion = plantillaDescuento.cod_fecha_creacion,
                                cod_fecha_expiracion = plantillaDescuento.cod_fecha_expiracion,
                                cod_utilizado = false,
                                UsuarioId = usuario.usu_id
                            };

                            _context.CodigosDescuento.Add(codigoUnico);
                            codigosCreados++;

                            // Enviar email al usuario
                            try
                            {
                                var htmlMessage = await CargarPlantillaCodigoDescuento(
                                    $"{usuario.usu_nombre} {usuario.usu_apellido}",
                                    plantillaDescuento.cod_codigo,
                                    plantillaDescuento.cod_descripcion,
                                    plantillaDescuento.cod_porcentaje,
                                    plantillaDescuento.cod_fecha_expiracion,
                                    plantillaDescuento.Autoparte?.aut_nombre ?? "Producto"
                                );

                                if (!string.IsNullOrEmpty(htmlMessage))
                                {
                                    await _emailSender.SendEmailAsync(
                                        usuario.usu_correo,
                                        "🎉 ¡Código de Descuento Exclusivo para Ti! - Marimon Autopartes",
                                        htmlMessage
                                    );

                                    emailsEnviados++;
                                }
                                else
                                {
                                    _logger.LogWarning($"No se pudo cargar la plantilla de email para {usuario.usu_correo}");
                                }
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, $"Error al enviar email a {usuario.usu_correo}");
                                // Continúa con el siguiente usuario aunque falle el envío del email
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Se crearon {codigosCreados} códigos únicos de descuento y se enviaron {emailsEnviados} emails a usuarios con más de 10 ventas.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar códigos de descuento personalizados");
                TempData["Error"] = "Ocurrió un error al enviar los códigos de descuento. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }


        private async Task<string> CargarPlantillaCodigoDescuento(string nombreUsuario, string codigoDescuento, 
            string descripcionDescuento, decimal porcentajeDescuento, DateTime fechaExpiracion, 
            string nombreProducto)
        {
            try
            {
                // Ruta a la plantilla HTML en Views/Emails/
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Emails", "CodigoDescuentoEmail.html");
                
                if (!System.IO.File.Exists(templatePath))
                {
                    _logger.LogError($"Plantilla de email no encontrada en: {templatePath}");
                    return string.Empty;
                }

                var htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);
                
                // Generar la URL del catálogo
                var catalogoUrl = Url.Action("Index", "Catalogo", null, Request.Scheme);
                
                // Reemplazar los placeholders con los valores reales
                var htmlMessage = htmlTemplate
                    .Replace("{nombreUsuario}", nombreUsuario)
                    .Replace("{codigoDescuento}", codigoDescuento)
                    .Replace("{descripcionDescuento}", descripcionDescuento)
                    .Replace("{porcentajeDescuento}", porcentajeDescuento.ToString("F0"))
                    .Replace("{nombreProducto}", nombreProducto)
                    .Replace("{fechaExpiracion}", fechaExpiracion.ToString("dd/MM/yyyy"))
                    .Replace("{catalogoUrl}", catalogoUrl);

                return htmlMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar plantilla de email para código de descuento");
                return string.Empty;
            }
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}