using FlightManager.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data;

/// <summary>
/// Represents the application database context that extends IdentityDbContext for user authentication.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the DbSet for Flight entities.
    /// </summary>
    public DbSet<Flight> Flights { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for Reservation entities.
    /// </summary>
    public DbSet<Reservation> Reservations { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for ReservationUser entities.
    /// </summary>
    public DbSet<ReservationUser> ReservationUsers { get; set; }

    /// <summary>
    /// Configures the model relationships and constraints for the database context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ReservationUser -> AppUser relationship
        modelBuilder.Entity<ReservationUser>()
            .HasOne(ru => ru.AppUser)
            .WithMany(u => u.ReservationUsers)
            .HasForeignKey(ru => ru.AppUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure ReservationUser -> Reservations relationship
        modelBuilder.Entity<ReservationUser>()
            .HasMany(ru => ru.Reservations)
            .WithOne(r => r.ReservationUser)
            .HasForeignKey(r => r.ReservationUserId)
            .OnDelete(DeleteBehavior.Cascade);

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

    /// <summary>
    /// Saves all changes made in this context to the database and cleans up orphaned ReservationUsers.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        var result = base.SaveChanges(); // Save initial changes

        // Remove orphaned ReservationUsers
        var orphanedUsers = ReservationUsers
            .Where(ru => !ru.Reservations.Any() && ru.AppUserId == null)
            .ToList();

        if (orphanedUsers.Any())
        {
            ReservationUsers.RemoveRange(orphanedUsers);
            base.SaveChanges();
        }

        return result;
    }
}