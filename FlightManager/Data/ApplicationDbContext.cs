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
    public DbSet<ReservationUser> ReservationUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ReservationUser -> AppUser relationship
        modelBuilder.Entity<ReservationUser>()
            .HasOne(ru => ru.AppUser)
            .WithMany(u => u.ReservationUsers)
            .HasForeignKey(ru => ru.AppUserId)
            .OnDelete(DeleteBehavior.SetNull); // Set null when AppUser is deleted

        // Configure ReservationUser -> Reservations relationship
        modelBuilder.Entity<ReservationUser>()
            .HasMany(ru => ru.Reservations)
            .WithOne(r => r.ReservationUser)
            .HasForeignKey(r => r.ReservationUserId)
            .OnDelete(DeleteBehavior.ClientNoAction); // Prevent cascade delete

        // Configure Reservation -> Flight relationship
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Flight)
            .WithMany(f => f.Reservations)
            .HasForeignKey(r => r.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure enum storage
        modelBuilder.Entity<Reservation>()
            .Property(r => r.TicketType)
            .HasConversion<string>()
            .HasMaxLength(12);
    }

    public override int SaveChanges()
    {
        var deletedReservations = ChangeTracker.Entries<Reservation>()
            .Where(e => e.State == EntityState.Deleted)
            .Select(e => e.Entity)
            .ToList();

        var result = base.SaveChanges(); // Save initial changes

        // Find orphaned users AFTER reservations are deleted
        var orphanedUsers = ReservationUsers
            .Where(ru =>
                !ru.Reservations.Any() &&
                ru.AppUserId == null &&
                deletedReservations.Any(dr => dr.ReservationUserId == ru.Id)
            )
            .ToList();

        if (orphanedUsers.Any())
        {
            ReservationUsers.RemoveRange(orphanedUsers);
            base.SaveChanges(); // Save orphan cleanup
        }

        return result;
    }
}