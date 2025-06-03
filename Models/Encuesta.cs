using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marimon.Models
{
    [Table("Encuesta")]
    public class Encuesta
    {
        [Key]
        public int encuesta_id { get; set; }

        [Required]
        public string agrado_sistema { get; set; }

        [Required]
        public string facilidad_pago { get; set; }

        [Range(0, 10)]
        public int nps_score { get; set; }

        public DateTime fecha_envio { get; set; } = DateTime.Now;

        public string? comentarios { get; set; }

        [Required]
        [ForeignKey("Usuario")]
        public string usu_id { get; set; }

        public Usuario Usuario { get; set; }
    }
}
