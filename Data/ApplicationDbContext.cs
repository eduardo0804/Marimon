using Marimon.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Marimon.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Autoparte> Autopartes { get; set; }

    public DbSet<Categoria> Categorias { get; set; } 
    public DbSet<Boleta> Boleta { get; set; } 
    public DbSet<Factura> Factura { get; set; } 
    public DbSet<Comprobante> Comprobante { get; set; } 
    public DbSet<DetalleVentas> DetalleVentas { get; set; } 
    public DbSet<Entradas> Entradas { get; set; } 
    public DbSet<MetodoPago> MetodoPago { get; set; } 
    public DbSet<Salida> Salida { get; set; } 
    public DbSet<Usuario> Usuario { get; set; } 
    public DbSet<Venta> Venta { get; set; } 
    public DbSet<Servicio> Servicio { get; set; }
    public DbSet<Reserva> Reserva { get; set; }
}
