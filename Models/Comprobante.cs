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
        public string tipo_comprobante { get; set; } 
        public int VentaId { get; set; }

        [ForeignKey("VentaId")]
        public Venta? Venta { get; set; }

        public string com_imagen { get; set; }
        public string com_evidencia { get; set; }

        public ICollection<Boleta>? Boletas { get; set; } = new List<Boleta>();
        public ICollection<Factura>? Facturas { get; set; } = new List<Factura>();
        public ICollection<Salida>? Salidas { get; set; } = new List<Salida>();

    }
}