using FlightManager.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace FlightManager.Test.ModelTests;

/// <summary>
/// Contains unit tests for validating the ReservationUser model's data annotations and business rules.
/// </summary>
[TestFixture]
public class ReservationUserTests
{
    /// <summary>
    /// Creates a valid ReservationUser instance for testing with all required fields populated.
    /// </summary>
    /// <returns>A valid ReservationUser instance with test data</returns>
    private static ReservationUser CreateValidUser()
    {
        return new ReservationUser
        {
            UserName = "TestUser123",
            FirstName = "John",
            MiddleName = "Michael",
            LastName = "Doe",
            EGN = "1234567890",
            Address = "123 Main Street, Sofia, Bulgaria",
            PhoneNumber = "+359887123456",
            Email = "test.user@example.com"
        };
    }

    /// <summary>
    /// Validates a model object and returns the validation results.
    /// </summary>
    /// <param name="model">The model instance to validate</param>
    /// <returns>List of validation results containing any validation errors</returns>
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    /// <summary>
    /// Tests that a fully valid ReservationUser passes all validation checks.
    /// </summary>
    [Test]
    public void ValidateReservationUser_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var user = CreateValidUser();

        // Act
        var results = ValidateModel(user);

        // Assert
        Assert.That(results, Is.Empty,
            $"Validation failed with errors: {string.Join(", ", results.Select(v => v.ErrorMessage))}");
    }

    /// <summary>
    /// Tests that validation fails when EGN has invalid format (not exactly 10 digits).
    /// </summary>
    [Test]
    public void ValidateReservationUser_ShouldFail_WhenEGNIsInvalid()
    {
        // Arrange
        var user = CreateValidUser();
        user.EGN = "12345"; // Invalid length

        // Act
        var results = ValidateModel(user);

        // Assert
        Assert.That(results.Any(v =>
            v.MemberNames.Contains(nameof(ReservationUser.EGN)) &&
            v.ErrorMessage == "EGN must be exactly 10 digits."),
            "Should validate EGN format");
    }

    /// <summary>
    /// Tests that validation fails when phone number has invalid format.
    /// </summary>
    [Test]
    public void ValidateReservationUser_ShouldFail_WhenPhoneNumberIsInvalid()
    {
        // Arrange
        var user = CreateValidUser();
        user.PhoneNumber = "123-abc-456"; // Invalid format

        // Act
        var results = ValidateModel(user);

        // Assert
        Assert.That(results.Any(v =>
            v.MemberNames.Contains(nameof(ReservationUser.PhoneNumber)) &&
            v.ErrorMessage == "Invalid phone number format."),
            "Should validate phone number format");
    }

    /// <summary>
    /// Tests that validation fails when email has invalid format.
    /// </summary>
    [Test]
    public void ValidateReservationUser_ShouldFail_WhenEmailIsInvalid()
    {
        // Arrange
        var user = CreateValidUser();
        user.Email = "invalid-email"; // Invalid format

        // Act
        var results = ValidateModel(user);

        // Assert
        Assert.That(results.Any(v =>
            v.MemberNames.Contains(nameof(ReservationUser.Email))),
            "Should validate email format when provided");
    }

    /// <summary>
    /// Tests that validation passes when email is not provided (since it's optional).
    /// </summary>
    [Test]
    public void ValidateReservationUser_ShouldPass_WhenEmailIsMissing()
    {
        // Arrange
        var user = CreateValidUser();
        user.Email = null; // Email is optional

        // Act
        var results = ValidateModel(user);

        // Assert
        Assert.That(results.All(v => !v.MemberNames.Contains(nameof(ReservationUser.Email))),
            "Email should be optional");
    }

    /// <summary>
    /// Tests that validation fails when string fields exceed maximum length.
    /// </summary>
    [Test]
    public void ValidateReservationUser_ShouldFail_WhenFieldsExceedMaxLength()
    {
        // Arrange
        var user = CreateValidUser();
        var longString = new string('a', 51); // 51 characters (max is 50 for most fields)

        user.UserName = longString;
        user.FirstName = longString;
        user.MiddleName = longString;
        user.LastName = longString;
        user.Address = new string('a', 101); // 101 characters (max is 100)

        // Act
        var results = ValidateModel(user);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(results.Any(v =>
                v.MemberNames.Contains(nameof(ReservationUser.UserName)) &&
                v.ErrorMessage == "Username cannot exceed 50 characters."),
                "Should validate UserName length");

            Assert.That(results.Any(v =>
                v.MemberNames.Contains(nameof(ReservationUser.FirstName)) &&
                v.ErrorMessage == "First name cannot exceed 50 characters."),
                "Should validate FirstName length");

            Assert.That(results.Any(v =>
                v.MemberNames.Contains(nameof(ReservationUser.MiddleName)) &&
                v.ErrorMessage == "Middle name cannot exceed 50 characters."),
                "Should validate MiddleName length");

            Assert.That(results.Any(v =>
                v.MemberNames.Contains(nameof(ReservationUser.LastName)) &&
                v.ErrorMessage == "Last name cannot exceed 50 characters."),
                "Should validate LastName length");

            Assert.That(results.Any(v =>
                v.MemberNames.Contains(nameof(ReservationUser.Address)) &&
                v.ErrorMessage == "Address cannot exceed 100 characters."),
                "Should validate Address length");
        });
    }
}