using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marimon.Models
{
    [Table("Usuario")]
    public class Usuario
    {
        [Key, ForeignKey("IdentityUser")] // Esto indica que es clave foránea también
        public string usu_id { get; set; }

        public string? usu_nombre { get; set; }
        public string? usu_apellido { get; set; }
        public string? usu_nombrePerfil { get; set; }
        public string? usu_numIdentificacion { get; set; }
        public string? usu_correo { get; set; }
        // Propiedad de navegación
        public IdentityUser IdentityUser { get; set; }
    }
}
