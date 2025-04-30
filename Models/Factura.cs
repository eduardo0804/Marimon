using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
using Marimon.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
        public int ComprobanteId { get; set; }
        
        [ForeignKey("ComprobanteId")]
        public Comprobante? Comprobante { get; set; } 

    }
}