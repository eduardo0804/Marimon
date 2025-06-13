using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marimon.Models
{
    [Table("OrdenTrabajo")]
    public class OrdenTrabajo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrdenTrabajoId { get; set; }

        // Relación con Reserva (uno a uno)
        public int ReservaId { get; set; }

        [ForeignKey("ReservaId")]
        public Reserva Reserva { get; set; }

        // Personal asignado (Usuario con rol "Personal")
        public string? PersonalId { get; set; }

        [ForeignKey("PersonalId")]
        public Usuario? Personal { get; set; }

        // Autoparte utilizada (solo una por ahora)
        public int? AutoparteId { get; set; }

        [ForeignKey("AutoparteId")]
        public Autoparte? Autoparte { get; set; }

        // Cantidad usada de la autoparte
        [NotMapped]
        public int CantidadUsada { get; set; }

        // Detalles del trabajo realizado
        public string? Descripcion { get; set; }

    }
}
