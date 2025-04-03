using FlightManager.Data;
using FlightManager.Data.Models;
using FlightManager.Extensions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FlightManager.Test.ServiceTests;

/// <summary>
/// Unit tests for the <see cref="ReservationCleanupService"/> class.
/// </summary>
/// <remarks>
/// These tests verify the behavior of the reservation cleanup service including:
/// <list type="bullet">
/// <item><description>Removal of expired unconfirmed reservations</description></item>
/// <item><description>Proper handling of associated reservation users</description></item>
/// <item><description>Error handling and logging</description></item>
/// <item><description>Graceful shutdown behavior</description></item>
/// </list>
/// Tests use an in-memory database for isolation and speed.
/// </remarks>
[TestFixture]
public class ReservationCleanupServiceTests : IDisposable
{
    private Mock<ILogger<ReservationCleanupService>> _loggerMock;
    private IServiceProvider _serviceProvider;
    private ApplicationDbContext _dbContext;
    private ReservationCleanupSettings _settings;

    /// <summary>
    /// Sets up the test environment before each test method is executed.
    /// </summary>
    /// <remarks>
    /// Initializes:
    /// <list type="bullet">
    /// <item><description>Logger mock</description></item>
    /// <item><description>In-memory database</description></item>
    /// <item><description>Service provider with test dependencies</description></item>
    /// <item><description>Test configuration settings</description></item>
    /// </list>
    /// </remarks>
    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ReservationCleanupService>>();

        // Setup in-memory database
        var serviceCollection = new ServiceCollection();
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(dbOptions);
        serviceCollection.AddSingleton(_dbContext);

        // Setup settings
        _settings = new ReservationCleanupSettings
        {
            CheckIntervalMinutes = 1, // Shorter interval for testing
            ExpiryHours = 48
        };

        var optionsMock = new Mock<IOptions<ReservationCleanupSettings>>();
        optionsMock.Setup(x => x.Value).Returns(_settings);

        serviceCollection.AddSingleton(optionsMock.Object);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    /// <summary>
    /// Cleans up test resources after each test method is executed.
    /// </summary>
    public void Dispose()
    {
        _dbContext?.Database.EnsureDeleted();
        _dbContext?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Creates a test reservation with specified parameters.
    /// </summary>
    /// <param name="createdAt">The creation date/time of the reservation.</param>
    /// <param name="isConfirmed">Whether the reservation is confirmed.</param>
    /// <param name="flightId">The ID of the associated flight.</param>
    /// <param name="userId">The ID of the associated reservation user.</param>
    /// <returns>A new reservation instance with test data.</returns>
    private Reservation CreateTestReservation(DateTime createdAt, bool isConfirmed, int flightId, int userId)
    {
        return new Reservation
        {
            CreatedAt = createdAt,
            IsConfirmed = isConfirmed,
            FlightId = flightId,
            ReservationUserId = userId,
            Nationality = "TestNationality",
            TicketType = TicketType.Economy
        };
    }

    /// <summary>
    /// Creates a test reservation user with specified parameters.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <param name="appUserId">The associated application user ID (optional).</param>
    /// <returns>A new reservation user instance with test data.</returns>
    private ReservationUser CreateTestUser(int id, string appUserId = null)
    {
        return new ReservationUser
        {
            Id = id,
            UserName = "testuser",
            FirstName = "Test",
            MiddleName = "Middle",
            LastName = "User",
            EGN = "1234567890",
            Address = "123 Test St",
            PhoneNumber = "1234567890",
            AppUserId = appUserId
        };
    }

    /// <summary>
    /// Tests that the service removes expired unconfirmed reservations.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    /// <remarks>
    /// Verifies that:
    /// <list type="number">
    /// <item><description>Expired unconfirmed reservations are removed</description></item>
    /// <item><description>Valid unconfirmed reservations remain</description></item>
    /// <item><description>Confirmed reservations remain regardless of age</description></item>
    /// <item><description>The operation is properly logged</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public async Task ExecuteAsync_ShouldRemoveExpiredUnconfirmedReservations()
    {
        // Arrange
        var expiredReservation = CreateTestReservation(DateTime.UtcNow.AddHours(-49), false, 1, 1);
        var validReservation = CreateTestReservation(DateTime.UtcNow.AddHours(-1), false, 2, 2);
        var confirmedReservation = CreateTestReservation(DateTime.UtcNow.AddHours(-49), true, 3, 3);

        // Add test users since ReservationUserId is a foreign key
        var user1 = CreateTestUser(1);
        var user2 = CreateTestUser(2);
        var user3 = CreateTestUser(3);

        await _dbContext.ReservationUsers.AddRangeAsync(user1, user2, user3);
        await _dbContext.Reservations.AddRangeAsync(expiredReservation, validReservation, confirmedReservation);
        await _dbContext.SaveChangesAsync();

        // Verify setup - should have 3 reservations initially
        var initialCount = await _dbContext.Reservations.CountAsync();
        Assert.That(initialCount, Is.EqualTo(3), "Test setup failed");

        var service = new ReservationCleanupService(
            _loggerMock.Object,
            _serviceProvider,
            Options.Create(_settings));

        var cancellationTokenSource = new CancellationTokenSource();

        // Act - Run for one iteration
        var task = service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(500); // Increased delay to ensure processing completes
        cancellationTokenSource.Cancel();
        await task;

        // Assert
        var remainingReservations = await _dbContext.Reservations.ToListAsync();
        var remainingIds = remainingReservations.Select(r => r.Id).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(remainingIds, Does.Not.Contain(expiredReservation.Id),
                "Expired reservation should be removed");
            Assert.That(remainingIds, Does.Contain(validReservation.Id),
                "Valid reservation should remain");
            Assert.That(remainingIds, Does.Contain(confirmedReservation.Id),
                "Confirmed reservation should remain");
        });

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Found 1 expired reservations to clean up")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that the service removes orphaned reservation users when their last reservation is cleaned up.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ExecuteAsync_ShouldRemoveOrphanedReservationUsers()
    {
        // Arrange
        var orphanUser = CreateTestUser(1);
        var expiredReservation = CreateTestReservation(DateTime.UtcNow.AddHours(-49), false, 1, 1);
        expiredReservation.ReservationUser = orphanUser;

        await _dbContext.ReservationUsers.AddAsync(orphanUser);
        await _dbContext.Reservations.AddAsync(expiredReservation);
        await _dbContext.SaveChangesAsync();

        var service = new ReservationCleanupService(
            _loggerMock.Object,
            _serviceProvider,
            Options.Create(_settings));

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(100);
        cancellationTokenSource.Cancel();
        await task;

        // Assert
        var remainingUsers = await _dbContext.ReservationUsers.ToListAsync();
        Assert.That(remainingUsers, Does.Not.Contain(orphanUser));
    }

    /// <summary>
    /// Tests that the service preserves reservation users who have other active reservations.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ExecuteAsync_ShouldKeepReservationUsersWithOtherReservations()
    {
        // Arrange
        var user = CreateTestUser(1);
        var expiredReservation = CreateTestReservation(DateTime.UtcNow.AddHours(-49), false, 1, 1);
        var validReservation = CreateTestReservation(DateTime.UtcNow, true, 2, 1);

        expiredReservation.ReservationUser = user;
        validReservation.ReservationUser = user;

        await _dbContext.ReservationUsers.AddAsync(user);
        await _dbContext.Reservations.AddRangeAsync(expiredReservation, validReservation);
        await _dbContext.SaveChangesAsync();

        var service = new ReservationCleanupService(
            _loggerMock.Object,
            _serviceProvider,
            Options.Create(_settings));

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(100);
        cancellationTokenSource.Cancel();
        await task;

        // Assert
        var remainingUsers = await _dbContext.ReservationUsers.ToListAsync();
        Assert.That(remainingUsers, Does.Contain(user));
    }

    /// <summary>
    /// Tests that the service preserves reservation users who are linked to application users.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ExecuteAsync_ShouldKeepAppUserLinkedReservationUsers()
    {
        // Arrange
        var user = CreateTestUser(1, "some-app-user-id");
        var expiredReservation = CreateTestReservation(DateTime.UtcNow.AddHours(-49), false, 1, 1);
        expiredReservation.ReservationUser = user;

        await _dbContext.ReservationUsers.AddAsync(user);
        await _dbContext.Reservations.AddAsync(expiredReservation);
        await _dbContext.SaveChangesAsync();

        var service = new ReservationCleanupService(
            _loggerMock.Object,
            _serviceProvider,
            Options.Create(_settings));

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(100);
        cancellationTokenSource.Cancel();
        await task;

        // Assert
        var remainingUsers = await _dbContext.ReservationUsers.ToListAsync();
        Assert.That(remainingUsers, Does.Contain(user));
    }

    /// <summary>
    /// Tests that the service properly logs errors when exceptions occur during cleanup.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ExecuteAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange - Create a service provider that will throw
        var badServiceCollection = new ServiceCollection();
        var badServiceProvider = badServiceCollection.BuildServiceProvider();

        var service = new ReservationCleanupService(
            _loggerMock.Object,
            badServiceProvider,
            Options.Create(_settings));

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(100);
        cancellationTokenSource.Cancel();
        await task;

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error occurred while cleaning up")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that the service stops gracefully when cancellation is requested.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    /// <remarks>
    /// Verifies that the service:
    /// <list type="number">
    /// <item><description>Properly logs startup</description></item>
    /// <item><description>Properly logs shutdown</description></item>
    /// <item><description>Handles cancellation without exceptions</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public async Task ExecuteAsync_ShouldStopGracefully_WhenCancelled()
    {
        // Arrange
        var service = new ReservationCleanupService(
            _loggerMock.Object,
            _serviceProvider,
            Options.Create(_settings));

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cancellationTokenSource.Token);

        // Give it a moment to start
        await Task.Delay(100);

        // Cancel and wait for completion
        cancellationTokenSource.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        // Allow some time for the stopping message to be logged
        await Task.Delay(100);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Reservation Cleanup Service is starting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Reservation Cleanup Service is stopping")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}