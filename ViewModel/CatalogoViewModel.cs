using System.Collections.Generic;

namespace Marimon.Models
{
    public class CatalogoViewModel
    {
        public List<Autoparte> Autopartes { get; set; }
        public Carrito Carrito { get; set; }

        // Propiedad para manejar la paginación actual (la página que se está visualizando)
        public int PaginaActual { get; set; }

        // Propiedad para manejar la cantidad total de páginas para la paginación
        public int TotalPaginas { get; set; }

        // Propiedad para manejar el término de búsqueda (si lo hay)
        public string Buscar { get; set; }
        
    }
}
