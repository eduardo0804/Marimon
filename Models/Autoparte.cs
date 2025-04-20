using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
    [Table("Autoparte")]
    public class Autoparte 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int aut_id { get; set; }
        public string? aut_nombre { get; set; }
        public string? aut_descripcion { get; set; }
        public decimal aut_precio { get; set; }
        public string? aut_especificacion { get; set; }
        public int aut_cantidad {get;set;}= 0;
        public string aut_imagen { get; set; }  = string.Empty;
        public int CategoriaId { get; set; } 


        [ForeignKey("CategoriaId")]
        public Categoria? Categoria { get; set; }
        
    }
}
