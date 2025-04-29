using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("Venta")]
    public class Venta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ven_id { get; set; }
         public DateOnly ven_fecha { get; set; }
        public int MetodoPagoId { get; set; }

        [ForeignKey("MetodoPagoId")]
        public MetodoPago? MetodoPago { get; set; }

        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        public decimal Total {get; set; }
        // Agregar esta línea 👇
        public ICollection<DetalleVentas> Detalles { get; set; } = new List<DetalleVentas>();

    }
}