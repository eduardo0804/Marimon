using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
    [Table("Servicio")]
    public class Servicio
    {
        [Key]
        public int ser_id { get; set; }

        public string ser_nombre { get; set; }
        public string ser_descripcion { get; set; }
        public string ser_img1 { get; set; }
        public string ser_img2 { get; set; }

        public virtual ICollection<Reserva> Reservas { get; set; }
    }
}
