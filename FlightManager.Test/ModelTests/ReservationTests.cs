using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FlightManager.Test.ModelTests;

/// <summary>
/// Contains unit tests for validating the Reservation model's business rules and database operations.
/// </summary>
[TestFixture]
public class ReservationTests
{
    private ApplicationDbContext _context;
    private ReservationUser _testReservationUser;
    private Flight _testFlight;

    /// <summary>
    /// Initializes the test environment with an in-memory database and test data.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
            .Options;

        _context = new ApplicationDbContext(options);

        // Create test flight
        _testFlight = new Flight
        {
            FromLocation = "New York",
            ToLocation = "London",
            DepartureTime = DateTime.Now.AddDays(1),
            ArrivalTime = DateTime.Now.AddDays(2),
            AircraftType = "Boeing 747",
            AircraftNumber = "747-100",
            PilotName = "John Doe",
            PassengerCapacity = 200,
            BusinessClassCapacity = 50
        };

        // Create test user
        _testReservationUser = new ReservationUser
        {
            UserName = "testuser",
            FirstName = "John",
            MiddleName = "Wonka",
            LastName = "Doe",
            EGN = "1234567890",
            Address = "123 Main St",
            PhoneNumber = "555-1234",
            Email = "test@example.com"
        };

        _context.Flights.Add(_testFlight);
        _context.ReservationUsers.Add(_testReservationUser);
        _context.SaveChanges();
    }

    /// <summary>
    /// Tests that a valid reservation passes all validation checks.
    /// </summary>
    [Test]
    public async Task ValidateReservation_ShouldPass_WhenAllRequirementsAreMet()
    {
        // Arrange
        var reservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(reservation, new ServiceProviderStub(_context), null);
        bool isValid = Validator.TryValidateObject(reservation, context, validationResults, true);

        // Assert
        Assert.That(isValid, Is.True,
            $"Validation failed with errors: {string.Join(", ", validationResults.Select(v => v.ErrorMessage))}");
    }

    /// <summary>
    /// Tests that validation fails when attempting to create a duplicate reservation.
    /// </summary>
    [Test]
    public void ValidateReservation_ShouldFail_WhenDuplicateReservationExists()
    {
        // Arrange - Create initial reservation
        var initialReservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy,
            CreatedAt = DateTime.UtcNow
        };
        _context.Reservations.Add(initialReservation);
        _context.SaveChanges();

        // Act - Attempt duplicate reservation
        var duplicateReservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Business, // Different ticket type but same user+flight
            CreatedAt = DateTime.UtcNow
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(duplicateReservation, new ServiceProviderStub(_context), null);
        Validator.TryValidateObject(duplicateReservation, validationContext, validationResults, true);

        // Also trigger IValidatableObject validation
        var customValidationResults = duplicateReservation.Validate(validationContext).ToList();
        validationResults.AddRange(customValidationResults);

        // Assert
        Assert.That(validationResults.Any(v =>
            v.ErrorMessage == "This user already has a reservation for this flight."),
            "Expected duplicate reservation validation error");
    }

    /// <summary>
    /// Tests that validation fails when the database context is unavailable.
    /// </summary>
    [Test]
    public void ValidateReservation_ShouldFail_WhenDatabaseContextIsUnavailable()
    {
        // Arrange
        var reservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy,
            CreatedAt = DateTime.UtcNow
        };

        // Act - No service provider provided
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(reservation, null, null);
        Validator.TryValidateObject(reservation, validationContext, validationResults, true);

        // Also trigger IValidatableObject validation
        var customValidationResults = reservation.Validate(validationContext).ToList();
        validationResults.AddRange(customValidationResults);

        // Assert
        Assert.That(validationResults.Any(v =>
            v.ErrorMessage == "Database context is unavailable."),
            "Expected database context unavailable error");
    }

    /// <summary>
    /// Tests that the CreatedAt property is automatically set when not provided.
    /// </summary>
    [Test]
    public void Reservation_ShouldSetCreatedAt_WhenNotProvided()
    {
        // Arrange
        var reservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy
        };

        // Act
        _context.Reservations.Add(reservation);
        _context.SaveChanges();

        // Assert
        Assert.That(reservation.CreatedAt, Is.Not.EqualTo(default(DateTime)),
            "CreatedAt should be set automatically");
    }

    /// <summary>
    /// Tests that IsConfirmed defaults to false for new reservations.
    /// </summary>
    [Test]
    public void Reservation_ShouldDefaultToNotConfirmed_WhenCreated()
    {
        // Arrange & Act
        var reservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy
        };

        // Assert
        Assert.That(reservation.IsConfirmed, Is.False,
            "New reservations should default to not confirmed");
    }

    /// <summary>
    /// Cleans up the test environment after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    /// <summary>
    /// Service provider stub for providing the database context during validation.
    /// </summary>
    private class ServiceProviderStub : IServiceProvider
    {
        private readonly ApplicationDbContext _dbContext;

        public ServiceProviderStub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(ApplicationDbContext) ? _dbContext : null;
        }
    }
}