using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;


namespace Marimon.Models
{
    [Table("Resenias")]
    public class Resenia
    {
        [Key]
        public int res_id { get; set; }
        public int res_puntuacion { get; set; }
        public string res_gusto { get; set; }
        public string res_comentario { get; set; }
        public DateTime res_fecha { get; set; }
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [ForeignKey("Servicio")]
        public int? ser_id { get; set; }
        public virtual Servicio? Servicio { get; set; }

        [ForeignKey("Autoparte")]
        public int? aut_id { get; set; }
        public virtual Autoparte? Autoparte { get; set; }
    }

}