using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("MetodoPago")]
    public class MetodoPago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int pag_id { get; set; }
        public string? pag_importe { get; set; }
        public string? pag_metodo { get; set; }
         public DateOnly pag_fecha { get; set; }

    }
}