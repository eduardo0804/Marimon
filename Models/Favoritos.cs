using System.ComponentModel.DataAnnotations;
using Marimon.Models;

public class Favoritos
{
    [Key]
    public int fav_id { get; set; }

    [Required]
    public int AutoparteId { get; set; }

    [Required]
    public string UsuarioId { get; set; }

    // Propiedades de navegación
    public virtual Autoparte Autoparte { get; set; }
    public virtual Usuario Usuario { get; set; }
}