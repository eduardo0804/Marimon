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
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Reserva>()
            .HasOne(r => r.Usuario)
            .WithMany(u => u.Reservas)
            .HasForeignKey(r => r.UsuarioId)
            .HasPrincipalKey(u => u.usu_id);

        builder.Entity<Reserva>()
            .Property(r => r.Estado)
            .HasConversion<string>();

        builder.Entity<Reclamacion>()
            .Property(r => r.TipoReclamacion)
            .HasConversion<string>();

        builder.Entity<Reclamacion>()
            .Property(r => r.TipoEntidad)
            .HasConversion<string>();

        builder.Entity<Reclamacion>()
            .Property(r => r.Estado)
            .HasConversion<string>();

        // Configuraciones para las nuevas entidades
        builder.Entity<Oferta>()
            .HasOne(o => o.Autoparte)
            .WithMany(a => a.Ofertas)
            .HasForeignKey(o => o.AutoparteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CodigoDescuento>()
            .HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CodigoDescuento>()
            .HasOne(c => c.Autoparte)
            .WithMany(a => a.CodigosDescuento)
            .HasForeignKey(c => c.AutoparteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Oferta>()
            .Property(o => o.ofe_fecha_inicio)
            .HasColumnType("date");

        builder.Entity<Oferta>()
            .Property(o => o.ofe_fecha_fin)
            .HasColumnType("date");

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
    public DbSet<Reclamacion> Reclamacion { get; set; } = null!;
    public DbSet<Resenia> Resenias { get; set; }
    
    public DbSet<Oferta> Ofertas { get; set; }
    public DbSet<CodigoDescuento> CodigosDescuento { get; set; }
}