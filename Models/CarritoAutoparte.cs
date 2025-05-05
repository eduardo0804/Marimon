using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
    [Table("CarritoAutoparte")]
    public class CarritoAutoparte
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int carAut_id { get; set; }
        public int CarritoId { get; set; }

        [ForeignKey("CarritoId")]
        public Carrito? Carrito { get; set; }

        public int AutoparteId { get; set; }

        [ForeignKey("AutoparteId")]
        public Autoparte? Autoparte { get; set; }

        public int car_cantidad { get; set; } = 0;

        public decimal car_subtotal { get; set; } = 0;  
    }

}
