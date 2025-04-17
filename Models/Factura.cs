using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{

    [Table("Factura")]
    public class Factura
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int fac_id { get; set; }
        public string? fac_razonsocial { get; set; }
        public string? fac_ruc { get; set; }
        public string? fac_direccion { get; set; }
        public int usu_id { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

    }
}