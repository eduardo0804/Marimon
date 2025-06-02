using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marimon.ViewModel;

namespace Marimon.Models.ViewModels
{
    public class AutoparteViewModel
    {
        public int aut_id { get; set; }
        public string aut_nombre { get; set; }
        public string aut_descripcion { get; set; }
        public string aut_especificacion { get; set; }
        public decimal aut_precio { get; set; }
        public int aut_cantidad { get; set; }
        public string aut_imagen { get; set; }
        public string CategoriaNombre { get; set; }
        public List<AutoparteViewModel> ProductosSimilares { get; set; }
        public List<ReseniaViewModel> Resenias { get; set; } // Cambiado a ReseniaViewModel

        // Campos de oferta
        public decimal? PorcentajeOferta { get; set; }
        public decimal? PrecioOferta { get; set; }
        public string? DescripcionOferta { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool? OfertaActiva { get; set; }
        public int? OfertaId { get; set; }

        // Campos de código de descuento
        public decimal? PorcentajeDescuento { get; set; }
        public decimal? PrecioDescuento { get; set; }
        public string? CodigoDescuento { get; set; }
        public string? DescripcionDescuento { get; set; }
        public DateTime? FechaInicioDescuento { get; set; }
        public DateTime? FechaFinDescuento { get; set; }
        public bool? DescuentoActivo { get; set; }
        public int? CodigoDescuentoId { get; set; }
        public bool? TieneDescuento { get; set; }
    }
}