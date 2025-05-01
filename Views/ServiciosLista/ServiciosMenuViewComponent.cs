using Microsoft.AspNetCore.Mvc;
using Marimon.Data;
using Microsoft.EntityFrameworkCore;

namespace Marimon.ViewComponents
{
    public class ServiciosMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public ServiciosMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var servicios = await _context.Servicio
                .OrderBy(s => s.ser_id)
                .ToListAsync();

            return View("~/Views/ServiciosLista/ServiciosMenu.cshtml", servicios);
        }
    }
}