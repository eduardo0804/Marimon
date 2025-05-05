using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marimon.Models;

namespace Marimon.ViewModel
{
    public class ServicioReservaViewModel
    {
        public List<Servicio> Servicios { get; set; }
        public List<Reserva> Reservas { get; set; }
    }
}