using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("Boleta")]
    public class Boleta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int bol_id { get; set; }
        public int ComprobanteId { get; set; }

        [ForeignKey("ComprobanteId")]
        public Comprobante? Comprobante { get; set; }

    }
}