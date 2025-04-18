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
    public DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Usuario>()
            .HasOne<IdentityUser>(u => u.IdentityUser)
            .WithOne()
            .HasForeignKey<Usuario>(u => u.usu_id)
            .OnDelete(DeleteBehavior.Cascade); // Esta línea hace que se elimine en cascada
    }
    
}
