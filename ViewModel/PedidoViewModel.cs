using System;

namespace Marimon.ViewModel
{
    public class PedidoDetalleViewModel
    {
        public string NombreAutoparte { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitarioFinal { get; set; }
        public decimal SubtotalFinal { get; set; }
        public string TipoDescuento { get; set; } // "Oferta", "Cupón", "Ninguno"
        public string DescripcionDescuento { get; set; } // Descripción de la oferta o cupón
        public string ImagenAutoparte { get; set; }
    }

    public class PedidoViewModel
    {
        public int PedidoId { get; set; }
        public DateOnly Fecha { get; set; }
        public string Estado { get; set; }
        public string MetodoPago { get; set; }
        public List<PedidoDetalleViewModel> Detalles { get; set; } = new();
        public decimal Total { get; set; }
    }
}
