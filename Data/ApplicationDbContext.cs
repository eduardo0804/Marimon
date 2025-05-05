using Marimon.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Marimon.Models;
using Microsoft.AspNetCore.Identity;

namespace Marimon.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public static string Unaccent(string input) => throw new NotSupportedException();
    public DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(Unaccent))!)
            .HasName("unaccent");

        builder.Entity<Usuario>()
            .HasOne<IdentityUser>(u => u.IdentityUser)
            .WithOne()
            .HasForeignKey<Usuario>(u => u.usu_id)
            .OnDelete(DeleteBehavior.Cascade); // Esta línea hace que se elimine en cascada
        
        builder.Entity<Reserva>()
            .HasOne(r => r.Usuario)
            .WithMany(u => u.Reservas)
            .HasForeignKey(r => r.UsuarioId)
            .HasPrincipalKey(u => u.usu_id); 

        builder.Entity<Reserva>()
            .Property(r => r.Estado)
            .HasConversion<string>();

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
    public DbSet<Carrito> Carritos { get; set; }
    public DbSet<CarritoAutoparte> CarritoAutopartes { get; set; } = null!;
}
