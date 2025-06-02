using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marimon.ViewModel
{
    public class ReseniaViewModel
    {
        public int res_id { get; set; }
        public string res_comentario { get; set; }
        public int res_puntuacion { get; set; }
        public DateTime res_fecha { get; set; }
        public string res_gusto { get; set; }
        public string usuario_id { get; set; }
        public string usuario_nombre { get; set; }
        public int aut_id { get; set; }
        public int? AutoparteId { get; set; }
    }
}