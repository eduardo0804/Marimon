using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Marimon.Models;

namespace Marimon.Models
{
	[Table("Reserva")]
	public class Reserva
	{
		[Key]
		public int res_id { get; set; }
		public string res_placa { get; set; }
		public string res_telefono { get; set; }
		public DateTime res_fecha { get; set; }
        public TimeSpan res_hora { get; set; }

        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

		[ForeignKey("Servicio")]
		public int ser_id { get; set; }
		public virtual Servicio Servicio { get; set; }
	}
}