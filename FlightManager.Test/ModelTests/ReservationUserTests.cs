using FlightManager.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace FlightManager.Test.ModelTests;

/// <summary>
/// Contains unit tests for the ReservationUser model validation.
/// </summary>
[TestFixture]
public class ReservationUserTests
{
    /// <summary>
    /// Creates a valid ReservationUser instance for testing.
    /// </summary>
    /// <returns>A valid ReservationUser instance</returns>
    private ReservationUser CreateValidUser()
    {
        return new ReservationUser
        {
            UserName = "TestUser",
            FirstName = "John",
            MiddleName = "M.",
            LastName = "Doe",
            EGN = "1234567890",
            Address = "123 Main Street, City, Country",
            PhoneNumber = "+1234567890"
        };
    }

    /// <summary>
    /// Validates a model object and returns the validation results.
    /// </summary>
    /// <param name="model">The model to validate</param>
    /// <returns>List of validation results</returns>
    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    /// <summary>
    /// Tests that a valid ReservationUser passes validation.
    /// </summary>
    [Test]
    public void ReservationUser_ValidData_ShouldPassValidation()
    {
        var user = CreateValidUser();
        var results = ValidateModel(user);
        Assert.That(results, Is.Empty);
    }

    /// <summary>
    /// Tests that validation fails when EGN has invalid format.
    /// </summary>
    [Test]
    public void ReservationUser_EGN_InvalidFormat_ShouldFailValidation()
    {
        var user = CreateValidUser();
        user.EGN = "12345"; // Invalid length
        var results = ValidateModel(user);
        Assert.That(results.Any(v => v.ErrorMessage == "EGN must be exactly 10 digits."), Is.True);
    }

    /// <summary>
    /// Tests that validation fails when phone number has invalid format.
    /// </summary>
    [Test]
    public void ReservationUser_PhoneNumber_InvalidFormat_ShouldFailValidation()
    {
        var user = CreateValidUser();
        user.PhoneNumber = "123abc"; // Invalid format
        var results = ValidateModel(user);
        Assert.That(results.Any(v => v.ErrorMessage == "Invalid phone number format."), Is.True);
    }
}