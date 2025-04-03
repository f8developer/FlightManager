using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightManager.Data.Models;

/// <summary>
/// Represents a reservation for a flight.
/// </summary>
public class Reservation : IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier of this reservation.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reservation was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the user who made this reservation.
    /// </summary>
    [Required(ErrorMessage = "Reservation user ID is required.")]
    public required int ReservationUserId { get; set; }

    /// <summary>
    /// Gets or sets the associated ReservationUser object.
    /// </summary>
    public ReservationUser? ReservationUser { get; set; }

    /// <summary>
    /// Gets or sets the ID of the flight for this reservation.
    /// </summary>
    [Required(ErrorMessage = "Flight ID is required.")]
    public int FlightId { get; set; }

    /// <summary>
    /// Gets or sets the associated Flight object.
    /// </summary>
    public Flight? Flight { get; set; }

    /// <summary>
    /// Gets or sets the nationality of this reservation's passenger.
    /// </summary>
    [Required(ErrorMessage = "Nationality is required.")]
    [StringLength(50, ErrorMessage = "Nationality cannot exceed 50 characters.")]
    public required string Nationality { get; set; }

    /// <summary>
    /// Gets or sets the type of ticket for this reservation.
    /// </summary>
    [Required(ErrorMessage = "Ticket type is required.")]
    [Column(TypeName = "nvarchar(12)")]
    public TicketType TicketType { get; set; }

    /// <summary>
    /// Gets or sets whether the reservation has been confirmed via email.
    /// </summary>
    public bool IsConfirmed { get; set; } = false;

    /// <summary>
    /// Gets or sets the confirmation token for email verification.
    /// </summary>
    [StringLength(64)]
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reservation was confirmed.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Validates the current object and returns any errors as a collection of ValidationResult objects.
    /// </summary>
    /// <param name="validationContext">The validation context for this operation.</param>
    /// <returns>A collection of ValidationResult objects representing any validation errors that occurred during processing.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Get the database context from the services
        var dbContext = validationContext.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;

        if (dbContext == null)
        {
            yield return new ValidationResult("Database context is unavailable.", new[] { nameof(FlightId) });
            yield break;
        }

        // Check for duplicate reservations with the same user ID and flight ID
        bool duplicateExists = dbContext.Reservations
            .Any(r => r.FlightId == FlightId &&
                      r.ReservationUserId == ReservationUserId &&
                      r.Id != Id);

        if (duplicateExists)
        {
            yield return new ValidationResult(
                "This user already has a reservation for this flight.",
                new[] { nameof(FlightId), nameof(ReservationUserId) });
        }
    }
}