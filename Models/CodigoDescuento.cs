using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
    [Table("CodigoDescuento")]
    public class CodigoDescuento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int cod_id { get; set; }
        
        public string cod_codigo { get; set; } = string.Empty;
        public decimal cod_porcentaje { get; set; }
        public DateTime cod_fecha_creacion { get; set; }
        public DateTime cod_fecha_expiracion { get; set; }
        public bool cod_utilizado { get; set; } = false;
        public string cod_descripcion { get; set; } = string.Empty;
        
        // FK al usuario que puede usar este código
        public string? UsuarioId { get; set; }
        
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
        
        // Si es nulo, aplica a cualquier producto. Si tiene valor, es específico para ese producto
        public int? AutoparteId { get; set; }
        
        [ForeignKey("AutoparteId")]
        public Autoparte? Autoparte { get; set; }
    }
}
