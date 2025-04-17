using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marimon.Models
{
    [Table("t_usuario")]
    public class Usuario
    {
        [Key]
        public string usu_id { get; set; }  // Será igual al ID de AspNetUsers (que es string)
        
        public string? usu_nombre { get; set; }
        
        public string? usu_apellido { get; set; }
        
        public string? usu_dni { get; set; }
        
        public string? usu_correo { get; set; }
        
        public bool? usu_correo_verificado { get; set; }
        
        public string? usu_contrasenia { get; set; }
        
        public string? usu_selloseg { get; set; }
        
        public string? usu_cierrepat { get; set; }
        
        public bool? usu_bloqueohab { get; set; }
        
        public int? usu_recuento { get; set; }
    }
}