using System.ComponentModel.DataAnnotations;
using Marimon.Models;

public class Favorito
{
    public int Id { get; set; }

    [Required]
    public int AutoparteId { get; set; }

    [Required]
    public string UsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Propiedades de navegación
    public virtual Autoparte Autoparte { get; set; }
    public virtual Usuario Usuario { get; set; }
}