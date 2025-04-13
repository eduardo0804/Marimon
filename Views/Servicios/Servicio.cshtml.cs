using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Marimon.Views.Servicios
{
    public class Servicio : PageModel
    {
        private readonly ILogger<Servicio> _logger;

        public Servicio(ILogger<Servicio> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}