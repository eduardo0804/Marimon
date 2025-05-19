using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
    [Table("Oferta")]
    public class Oferta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ofe_id { get; set; }
        
        public decimal ofe_porcentaje { get; set; }
        public DateTime ofe_fecha_inicio { get; set; }
        public DateTime ofe_fecha_fin { get; set; }
        public string ofe_descripcion { get; set; } = string.Empty; //ejemplo "Promoción verano 2025"
        public bool ofe_activa { get; set; } = true;
        
        // FK a la autoparte
        public int AutoparteId { get; set; }
        
        [ForeignKey("AutoparteId")]
        public Autoparte? Autoparte { get; set; }
    }
}
