using System.ComponentModel.DataAnnotations;

namespace FlightManager.Data.Models;

/// <summary>
/// Represents a flight with its associated properties and validation rules.
/// </summary>
public class Flight : IValidatableObject
{
    /// <summary>
    /// Unique identifier for this flight record.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The departure location of the flight. This field is required.
    /// </summary>
    [Required(ErrorMessage = "Departure location is required.")]
    public required string FromLocation { get; set; }

    /// <summary>
    /// The destination location of the flight. This field is required.
    /// </summary>
    [Required(ErrorMessage = "Destination location is required.")]
    public required string ToLocation { get; set; }

    /// <summary>
    /// The departure time of the flight. This field is required.
    /// </summary>
    [Required(ErrorMessage = "Departure time is required.")]
    public DateTime DepartureTime { get; set; }

    /// <summary>
    /// The arrival time of the flight. This field is required.
    /// </summary>
    [Required(ErrorMessage = "Arrival time is required.")]
    public DateTime ArrivalTime { get; set; }

    /// <summary>
    /// The type of aircraft used for this flight. This field is required.
    /// </summary>
    [Required(ErrorMessage = "Aircraft type is required.")]
    public required string AircraftType { get; set; }

    /// <summary>
    /// The registration number of the aircraft used for this flight. This field is required.
    /// </summary>
    [Required(ErrorMessage = "Aircraft number is required.")]
    public required string AircraftNumber { get; set; }

    /// <summary>
    /// The name of the pilot assigned to this flight. This field is required.
    /// </summary>
    [Required(ErrorMessage = "Pilot name is required.")]
    public required string PilotName { get; set; }

    /// <summary>
    /// The maximum number of passengers that can be accommodated on this flight. This field is required and must be at least 1.
    /// </summary>
    [Required(ErrorMessage = "Passenger capacity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Passenger capacity must be at least 1.")]
    public int PassengerCapacity { get; set; }

    /// <summary>
    /// The maximum number of passengers that can be accommodated in business class on this flight. This field is required and cannot exceed the total passenger capacity.
    /// </summary>
    [Required(ErrorMessage = "Business class capacity is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Business class capacity cannot be negative.")]
    public int BusinessClassCapacity { get; set; }

    /// <summary>
    /// A collection of reservations associated with this flight.
    /// </summary>
    public ICollection<Reservation>? Reservations { get; set; }

    /// <summary>
    /// Validates the properties of this flight object.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ArrivalTime <= DepartureTime)
        {
            yield return new ValidationResult(
                "Arrival time must be after departure time.",
                new[] { nameof(ArrivalTime) });
        }

        if (BusinessClassCapacity > PassengerCapacity)
        {
            yield return new ValidationResult(
                "Business class capacity cannot exceed total passenger capacity.",
                new[] { nameof(BusinessClassCapacity) });
        }
    }
}