public class FavoritoDto
{
    public int Id { get; set; }
    public int AutoparteId { get; set; }
    public string AutoparteNombre { get; set; } = string.Empty;
    public string AutoparteCodigo { get; set; } = string.Empty;
    public decimal AutopartePrecio { get; set; }
    public string? AutoparteDescripcion { get; set; }
    public string? AutoparteImagen { get; set; }
    public bool EsFavorito { get; set; } = true;
}

public class FavoritosViewModel
{
    public List<FavoritoDto> Favoritos { get; set; } = new();
    public string? TerminoBusqueda { get; set; }
    public int TotalFavoritos { get; set; }
    public string? MensajeEstado { get; set; }
}