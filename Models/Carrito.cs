using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
    [Table("Carrito")]
    public class Carrito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int car_id { get; set; }
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        public decimal car_total {
            get { return CarritoAutopartes.Sum(c => c.car_subtotal); }
        }
        
    public class CarritoItem
    {
        public string Nombre { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
    }
    
        public ICollection<CarritoAutoparte> CarritoAutopartes { get; set; } = new List<CarritoAutoparte>();

    }
}
