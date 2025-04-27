using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marimon.Models.ViewModels
{
    public class AutoparteViewModel
    {
        public int aut_id { get; set; }
        public string aut_nombre { get; set; }
        public string aut_descripcion { get; set; }
        public string aut_especificacion { get; set; }
        public decimal aut_precio { get; set; }
        public int aut_cantidad {get;set;}
        public string aut_imagen { get; set; }
        public string CategoriaNombre { get; set; } 
        public List<AutoparteViewModel> ProductosSimilares { get; set; } = new List<AutoparteViewModel>();

    }
}