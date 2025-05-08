using System.ComponentModel.DataAnnotations;

namespace Marimon.Models
{
    public class PagoYapeViewModel
    {
        public string TipoComprobante { get; set; }
        public string NumIdentificacion { get; set; }
        public string RazonSocial { get; set; }
        public string Ruc { get; set; }
        public string Direccion { get; set; }
    }
}