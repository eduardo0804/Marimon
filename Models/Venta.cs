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
        public string? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        public decimal Total {get; set; }
        
        // Propiedad nueva para almacenar el ID de sesión de Stripe
        public string StripeSessionId { get; set; }
        
        public ICollection<DetalleVentas> Detalles { get; set; } = new List<DetalleVentas>();

        [NotMapped]
        public string? AutoParteNombre { get; internal set; }
        [NotMapped]
        public decimal AutoPartePrecio { get; internal set; }
        [NotMapped]
        public int Cantidad { get; internal set; }
    }
}