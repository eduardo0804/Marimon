using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("Comprobante")]
    public class Comprobante
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int com_id { get; set; }
        public string? com_nombre { get; set; }

        public int? FacturaId { get; set; }
        [ForeignKey("FacturaId")]
        public Factura? Factura { get; set; }
        public int? BoletaId { get; set; }
        [ForeignKey("BoletaId")]
        public Boleta? Boleta { get; set; }
        public int VentaId { get; set; }

        [ForeignKey("VentaId")]
        public Venta? Venta { get; set; }

    }
}