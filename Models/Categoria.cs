using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marimon.Models
{
    [Table("Categoria")]
    public class Categoria 
    {
        [Key]
        public int cat_id { get; set; }
        
        public string? cat_nombre { get; set; }

    }
}
