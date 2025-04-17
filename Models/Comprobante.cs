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

        public string? com_factura { get; set; }
        [ForeignKey("FacturaId")]
        public Factura? Factura { get; set; }
        public string? com_boleta { get; set; }
        [ForeignKey("BoletaId")]
        public Boleta? Boleta { get; set; }
        public int ven_id { get; set; }

        [ForeignKey("VentaId")]
        public Venta? Venta { get; set; }

    }
}