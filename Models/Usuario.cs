using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("Usuario")]
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int usu_id { get; set; }
        public string? usu_nombre { get; set; }
        public string? usu_apellido { get; set; }
        public string? usu_num_identificacion { get; set; }
        public string? usu_correo { get; set; }
        public string? usu_contrasena { get; set; }

    }
}