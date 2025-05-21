using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Marimon.Enums;

namespace Marimon.Models
{
    [Table("Reclamacion")]
    public class Reclamacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Required]
        public TipoReclamacion TipoReclamacion { get; set; }

        [Required]
        public TipoEntidad TipoEntidad { get; set; }

        [Required]
        public int EntidadId { get; set; } // Id del producto o servicio relacionado

        [Required]
        [StringLength(1000)]
        public string Descripcion { get; set; }

        [Required]
        public EstadoReclamacion Estado { get; set; } = EstadoReclamacion.Pendiente;

        public string? Respuesta { get; set; }

        public DateTime FechaReclamacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaRespuesta { get; set; }

        public string NumeroReferencia { get; set; } // Número de pedido o servicio

        public decimal Monto { get; set; }

        // Propiedad auxiliar solo para enviar/recibir nombre, no va a BD
        [NotMapped]
        public string NombreEntidad { get; set; }

    }
}