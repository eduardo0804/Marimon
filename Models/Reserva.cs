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
		public string res_fecha { get; set; }

		//[ForeignKey("User")]
		//public string user_id { get; set; }

		[ForeignKey("Servicio")]
		public int ser_id { get; set; }
		public virtual Servicio Servicio { get; set; }
	}
}