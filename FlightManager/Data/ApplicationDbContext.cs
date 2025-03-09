using FlightManager.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Flight> Flights { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Reservation -> AppUser relationship
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Adjust delete behavior as needed

        // Configure Reservation -> Flight relationship
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Flight)
            .WithMany(f => f.Reservations)
            .HasForeignKey(r => r.FlightId)
            .OnDelete(DeleteBehavior.Cascade); // Adjust delete behavior as needed
    }
}