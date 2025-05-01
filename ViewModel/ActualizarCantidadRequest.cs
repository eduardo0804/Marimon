namespace Marimon.Models.ViewModels
{
    public class ActualizarCantidadRequest
    {
        public int CarritoId { get; set; }
        public int ProductoId { get; set; }
        public int NuevaCantidad { get; set; }
    }
}
