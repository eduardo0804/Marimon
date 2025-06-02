public class FavoritosListViewModel
{
    public List<FavoritoViewModel> Favoritos { get; set; } = new List<FavoritoViewModel>();
    public string TerminoBusqueda { get; set; }
    public int TotalFavoritos { get; set; }
    public string MensajeEstado { get; set; }
}