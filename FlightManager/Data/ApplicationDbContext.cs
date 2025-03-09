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
}