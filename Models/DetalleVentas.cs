using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("DetalleVentas")]
    public class DetalleVentas
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int det_id { get; set; }
        public string? det_cantidad { get; set; }
        public int VentaId { get; set; }
        public int AutoParteId { get; set; }

        [ForeignKey("VentaId")]
        public Venta? Venta { get; set; }

        [ForeignKey("AutoParteId")]
        public Autoparte? Autoparte { get; set; }

    }
}