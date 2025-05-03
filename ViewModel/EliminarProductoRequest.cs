using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marimon.Models.ViewModels
{
    public class EliminarProductoRequest
    {
        public int CarritoId { get; set; }
        public int ProductoId { get; set; }
    }
}	