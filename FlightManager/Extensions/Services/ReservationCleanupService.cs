using FlightManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FlightManager.Extensions.Services;

/// <summary>
/// A background service that periodically checks for and removes expired reservations.
/// </summary>
/// <remarks>
/// This service runs in the background and performs the following operations at regular intervals:
/// <list type="bullet">
/// <item><description>Identifies reservations that exceed the configured expiry time without being confirmed</description></item>
/// <item><description>Removes expired reservations and their associated user data when appropriate</description></item>
/// <item><description>Logs cleanup operations and any errors that occur</description></item>
/// </list>
/// </remarks>
public class ReservationCleanupService : BackgroundService
{
    private readonly ILogger<ReservationCleanupService> _logger;
    private readonly IServiceProvider _services;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _reservationExpiryTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationCleanupService"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record service events and errors.</param>
    /// <param name="services">The service provider for creating scoped services.</param>
    /// <param name="settings">Configuration settings for the cleanup service.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/>, <paramref name="services"/>, or <paramref name="settings"/> is null.
    /// </exception>
    public ReservationCleanupService(
        ILogger<ReservationCleanupService> logger,
        IServiceProvider services,
        IOptions<ReservationCleanupSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _checkInterval = TimeSpan.FromMinutes(settings?.Value?.CheckIntervalMinutes ?? 10);
        _reservationExpiryTime = TimeSpan.FromHours(settings?.Value?.ExpiryHours ?? 48);
    }

    /// <summary>
    /// Executes the cleanup service, periodically checking for and removing expired reservations.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token that signals when the service should stop.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// The service performs the following steps in each iteration:
    /// <list type="number">
    /// <item><description>Creates a new service scope for database access</description></item>
    /// <item><description>Queries for unconfirmed reservations older than the expiry time</description></item>
    /// <item><description>Removes associated reservation users if they meet deletion criteria</description></item>
    /// <item><description>Deletes all expired reservations</description></item>
    /// <item><description>Waits for the configured interval before repeating</description></item>
    /// </list>
    /// Any exceptions during processing are caught and logged without stopping the service.
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Cleanup Service is starting.");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Find all unconfirmed reservations older than the expiry time
                        var expiryTime = DateTime.UtcNow - _reservationExpiryTime;
                        var expiredReservations = await dbContext.Reservations
                            .Include(r => r.ReservationUser)
                            .Where(r => !r.IsConfirmed && r.CreatedAt < expiryTime)
                            .ToListAsync(stoppingToken);

                        if (expiredReservations.Any())
                        {
                            _logger.LogInformation($"Found {expiredReservations.Count} expired reservations to clean up.");

                            // Remove associated users if they have no other reservations and aren't linked to an app user
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

                            // Remove the expired reservations
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

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
        finally
        {
            _logger.LogInformation("Reservation Cleanup Service is stopping.");
        }
    }
}