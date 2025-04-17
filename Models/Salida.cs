using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("Salida")]
    public class Salida
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int sal_id { get; set; }
        public DateOnly sal_fechasalida { get; set; }
        public int aut_cantidad { get; set; }
        public int com_id { get; set; }

        [ForeignKey("ComprobanteId")]
        public Comprobante? Comprobante { get; set; }

    }
}