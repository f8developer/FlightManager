using FlightManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// A service responsible for cleaning up expired reservations.
/// </summary>
namespace FlightManager.Extensions.Services;

public class ReservationCleanupService : BackgroundService
{
    private readonly ILogger<ReservationCleanupService> _logger;
    private readonly IServiceProvider _services;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _reservationExpiryTime = TimeSpan.FromHours(48);

    /// <summary>
    /// Initializes a new instance of the ReservationCleanupService class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="services">The service provider to use for resolving services.</param>
    /// <param name="settings">The settings for reservation cleanup, including check interval and expiry time.</param>
    public ReservationCleanupService(
        ILogger<ReservationCleanupService> logger,
        IServiceProvider services,
        IOptions<ReservationCleanupSettings> settings)
    {
        _logger = logger;
        _services = services;
        _checkInterval = TimeSpan.FromMinutes(settings.Value.CheckIntervalMinutes);
        _reservationExpiryTime = TimeSpan.FromHours(settings.Value.ExpiryHours);
    }

    /// <summary>
    /// Executes the cleanup service, periodically checking for expired reservations.
    /// </summary>
    /// <param name="stoppingToken">The token used to signal cancellation of the task.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    /// <summary>
                    /// Finds all reservations that are older than the expiry time and have not been confirmed.
                    /// </summary>
                    var expiryTime = DateTime.UtcNow - _reservationExpiryTime;
                    var expiredReservations = await dbContext.Reservations
                        .Include(r => r.ReservationUser)
                        .Where(r => !r.IsConfirmed && r.CreatedAt < expiryTime)
                        .ToListAsync(stoppingToken);

                    if (expiredReservations.Any())
                    {
                        _logger.LogInformation($"Found {expiredReservations.Count} expired reservations to clean up.");

                        /// <summary>
                        /// Removes associated users if they have no other reservations and are not linked to an app user.
                        /// </summary>
                        foreach (var reservation in expiredReservations)
                        {
                            if (reservation.ReservationUser != null)
                            {
                                var hasOtherReservations = await dbContext.Reservations
                                    .AnyAsync(r => r.ReservationUserId == reservation.ReservationUserId &&
                                                  r.Id != reservation.Id, stoppingToken);

                                if (!hasOtherReservations && reservation.ReservationUser.AppUserId == null)
                                {
                                    dbContext.ReservationUsers.Remove(reservation.ReservationUser);
                                }
                            }
                        }

                        /// <summary>
                        /// Removes the expired reservations from the database.
                        /// </summary>
                        dbContext.Reservations.RemoveRange(expiredReservations);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation($"Successfully removed {expiredReservations.Count} expired reservations.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired reservations.");
            }

            /// <summary>
            /// Waits for the specified interval before checking again.
            /// </summary>
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Reservation Cleanup Service is stopping.");
    }
}
