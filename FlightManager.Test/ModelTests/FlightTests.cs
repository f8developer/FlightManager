using FlightManager.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace FlightManager.Test.ModelTests;

/// <summary>
/// Contains unit tests for the Flight model validation.
/// </summary>
[TestFixture]
public class FlightTests
{
    /// <summary>
    /// Tests that flight validation fails when arrival time is before departure time.
    /// </summary>
    [Test]
    public void FlightValidation_ShouldFail_WhenArrivalBeforeDeparture()
    {
        // Arrange
        var flight = new Flight
        {
            FromLocation = "New York",
            ToLocation = "Los Angeles",
            DepartureTime = DateTime.Now.AddHours(5),
            ArrivalTime = DateTime.Now.AddHours(3), // Invalid
            AircraftType = "Boeing 747",
            AircraftNumber = "BA123",
            PilotName = "John Doe",
            PassengerCapacity = 200,
            BusinessClassCapacity = 50
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(flight);

        // Act
        Validator.TryValidateObject(flight, context, validationResults, true);
        var validateResults = flight.Validate(context).ToList();
        validationResults.AddRange(validateResults);

        // Assert
        Assert.That(validationResults.Exists(v =>
                v.ErrorMessage == "Arrival time must be after departure time."),
            Is.True);
    }

    /// <summary>
    /// Tests that flight validation fails when business class capacity exceeds total passenger capacity.
    /// </summary>
    [Test]
    public void FlightValidation_ShouldFail_WhenBusinessClassExceedsTotalCapacity()
    {
        // Arrange
        var flight = new Flight
        {
            FromLocation = "New York",
            ToLocation = "Los Angeles",
            DepartureTime = DateTime.Now.AddHours(5),
            ArrivalTime = DateTime.Now.AddHours(8),
            AircraftType = "Boeing 747",
            AircraftNumber = "BA123",
            PilotName = "John Doe",
            PassengerCapacity = 50,
            BusinessClassCapacity = 100 // Invalid
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(flight);

        // Act
        Validator.TryValidateObject(flight, context, validationResults, true);
        var validateResults = flight.Validate(context).ToList();
        validationResults.AddRange(validateResults);

        // Assert
        Assert.That(validationResults.Exists(v =>
                v.ErrorMessage == "Business class capacity cannot exceed total passenger capacity."),
            Is.True);
    }

    /// <summary>
    /// Tests that flight validation passes with valid data.
    /// </summary>
    [Test]
    public void FlightValidation_ShouldPass_WithValidData()
    {
        // Arrange
        var flight = new Flight
        {
            FromLocation = "New York",
            ToLocation = "Los Angeles",
            DepartureTime = DateTime.Now.AddHours(5),
            ArrivalTime = DateTime.Now.AddHours(8),
            AircraftType = "Boeing 747",
            AircraftNumber = "BA123",
            PilotName = "John Doe",
            PassengerCapacity = 200,
            BusinessClassCapacity = 50
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(flight);

        // Act
        var isValid = Validator.TryValidateObject(flight, context, validationResults, true);

        // Assert
        Assert.That(isValid, Is.True);
    }
}