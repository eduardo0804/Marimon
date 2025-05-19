using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marimon.Models.ViewModels
{
    public class OfertasViewModel
    {
        public List<AutoparteViewModel> Autopartes { get; set; } = new List<AutoparteViewModel>();
        public int TotalProductosEnStock { get; set; }
    }
}