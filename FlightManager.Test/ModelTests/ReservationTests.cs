using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using FlightManager.Data;
using FlightManager.Data.Models;

namespace FlightManager.Test.ModelTests;

/// <summary>
/// Contains unit tests for the Reservation model validation and database operations.
/// </summary>
[TestFixture]
public class ReservationTests
{
    private ApplicationDbContext _context;
    private ReservationUser _testReservationUser;
    private Flight _testFlight;

    /// <summary>
    /// Sets up the test environment before each test method is executed.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        _context = new ApplicationDbContext(options);

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

        _testReservationUser = new ReservationUser
        {
            UserName = "testuser",
            MiddleName = "Wonka",
            FirstName = "John",
            LastName = "Doe",
            EGN = "1234567890",
            Address = "123 Main St",
            PhoneNumber = "555-1234"
        };

        _context.Flights.Add(_testFlight);
        _context.ReservationUsers.Add(_testReservationUser);
        _context.SaveChanges();
    }

    /// <summary>
    /// Tests that reservation validation passes when creating the first reservation.
    /// </summary>
    [Test]
    public async Task ValidateUniqueReservation_ShouldPass_WhenFirstReservationIsCreated()
    {
        var reservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(reservation, new ServiceProviderStub(_context), null);

        bool isValid = Validator.TryValidateObject(reservation, context, validationResults, true);

        Assert.That(isValid, Is.True, () => string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
    }

    /// <summary>
    /// Tests that reservation validation fails when a duplicate reservation exists.
    /// </summary>
    [Test]
    public void ValidateUniqueReservation_ShouldFail_WhenDuplicateReservationExists()
    {
        // Arrange
        var reservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy
        };
    
        _context.Reservations.Add(reservation);
        _context.SaveChanges();

        // Act
        var duplicateReservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Business
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(duplicateReservation, new ServiceProviderStub(_context), null);
        Validator.TryValidateObject(duplicateReservation, validationContext, validationResults, true);

        // Also trigger IValidatableObject validation
        var validateResults = duplicateReservation.Validate(validationContext).ToList();
        validationResults.AddRange(validateResults);

        // Assert
        Assert.That(validationResults.Any(v => 
                v.ErrorMessage == "This user already has a reservation for this flight."), 
            Is.True);
    }

    /// <summary>
    /// Tests that reservation validation fails when the database context is unavailable.
    /// </summary>
    [Test]
    public void ValidateUniqueReservation_ShouldFail_WhenDatabaseContextIsUnavailable()
    {
        // Arrange
        var reservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy
        };

        // Act - no service provider provided
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(reservation, null, null);
        Validator.TryValidateObject(reservation, validationContext, validationResults, true);

        // Also trigger IValidatableObject validation
        var validateResults = reservation.Validate(validationContext).ToList();
        validationResults.AddRange(validateResults);

        // Assert
        Assert.That(validationResults.Any(v => 
                v.ErrorMessage == "Database context is unavailable."), 
            Is.True);
    }

    /// <summary>
    /// Cleans up the test environment after each test method is executed.
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
    public class ServiceProviderStub : IServiceProvider
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