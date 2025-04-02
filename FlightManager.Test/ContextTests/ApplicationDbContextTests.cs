using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Test.ContextTests;

/// <summary>
/// Contains unit tests for the ApplicationDbContext class.
/// </summary>
[TestFixture]
public class ApplicationDbContextTests
{
    private ApplicationDbContext _context;
    private ReservationUser _testReservationUser;
    private Reservation _testReservation;
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

        // Seed required entities
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

        _testReservation = new Reservation
        {
            ReservationUserId = _testReservationUser.Id,
            FlightId = _testFlight.Id,
            Nationality = "American",
            TicketType = TicketType.Economy
        };

        _context.AddRange(_testFlight, _testReservationUser, _testReservation);
        _context.SaveChanges();
    }

    /// <summary>
    /// Tests that orphaned ReservationUsers are automatically removed when their last reservation is deleted.
    /// </summary>
    [Test]
    public async Task TestRemoveOrphanedReservationUser()
    {
        // Act - Remove the reservation, which should orphan the ReservationUser
        _context.Reservations.Remove(_testReservation);
        await _context.SaveChangesAsync();

        // Assert - Check if the ReservationUser is deleted
        var orphanedReservationUser = await _context.ReservationUsers
            .FirstOrDefaultAsync(ru => ru.Id == _testReservationUser.Id);

        Assert.That(orphanedReservationUser, Is.Null);
    }

    /// <summary>
    /// Tests that non-orphaned ReservationUsers are not removed when saving changes.
    /// </summary>
    [Test]
    public async Task TestSaveChanges_NoOrphanedReservationUser()
    {
        // Act - Save changes after creating a new ReservationUser without reservations
        var newReservationUser = new ReservationUser
        {
            UserName = "newuser",
            MiddleName = "Wonka",
            FirstName = "Jane",
            LastName = "Smith",
            EGN = "9876543210",
            Address = "456 Side St",
            PhoneNumber = "555-5678"
        };

        _context.ReservationUsers.Add(newReservationUser);
        await _context.SaveChangesAsync();

        // Assert - Ensure that the new ReservationUser is saved and not orphaned
        var savedUser = await _context.ReservationUsers
            .FirstOrDefaultAsync(ru => ru.UserName == "newuser");

        Assert.That(savedUser, Is.Not.Null);
    }

    /// <summary>
    /// Tests the relationship between ReservationUser and AppUser entities.
    /// </summary>
    [Test]
    public async Task TestReservationUserAppUserRelationship()
    {
        // Arrange
        var appUser = new AppUser { UserName = "appuser" };
        _context.Users.Add(appUser); // Add AppUser to context
        await _context.SaveChangesAsync(); // Save changes so AppUser gets a valid ID

        _testReservationUser.AppUserId = appUser.Id; // Assign AppUserId to ReservationUser
        _context.ReservationUsers.Add(_testReservationUser); // Add ReservationUser to context (make sure it is added, not updated)
        await _context.SaveChangesAsync(); // Save the changes

        // Act
        var retrievedUser = await _context.ReservationUsers
            .Include(ru => ru.AppUser) // Explicitly include AppUser
            .FirstOrDefaultAsync(ru => ru.Id == _testReservationUser.Id);

        // Assert
        Assert.That(retrievedUser?.AppUser, Is.Not.Null);
        Assert.That(retrievedUser?.AppUser?.UserName, Is.EqualTo("appuser"));
    }

    /// <summary>
    /// Tests that TicketType enum values are correctly stored and retrieved from the database.
    /// </summary>
    [Test]
    public async Task TestEnumStorage()
    {
        // Act
        var retrievedReservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == _testReservation.Id);

        // Assert
        Assert.That(retrievedReservation?.TicketType, Is.EqualTo(TicketType.Economy));
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
}