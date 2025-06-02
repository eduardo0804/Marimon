public class FavoritoViewModel
{
    public int Id { get; set; }
    public int AutoparteId { get; set; }
    public string AutoparteNombre { get; set; }
    public string AutoparteCodigo { get; set; }
    public decimal AutopartePrecio { get; set; }
    public string AutoparteDescripcion { get; set; }
    public string AutoparteImagen { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool EsFavorito { get; set; } = true;
}

public class FavoritosListViewModel
{
    public List<FavoritoViewModel> Favoritos { get; set; } = new List<FavoritoViewModel>();
    public string TerminoBusqueda { get; set; }
    public int TotalFavoritos { get; set; }
    public string MensajeEstado { get; set; }
}