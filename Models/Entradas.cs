using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
    [Table("Entradas")]
    public class Entradas 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ent_id { get; set; }
        public int ent_cantidad { get; set; }
        public string? ent_proveedor { get; set; }
        public decimal aut_precio { get; set; }
        public DateOnly ent_fechaent { get; set; }
        public int aut_cantidad {get;set;}
        public int aut_id { get; set; } 

        [ForeignKey("AutoparteId")]
        public Autoparte? Autoparte { get; set; }
        
    }
}
