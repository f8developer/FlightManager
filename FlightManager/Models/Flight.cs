using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models;

public class Flight
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string FromLocation { get; set; }

    [Required]
    public required string ToLocation { get; set; }

    [Required]
    public required DateTime DepartureTime { get; set; }

    [Required]
    [CustomValidation(typeof(Flight), nameof(ValidateFlightTimes))]
    public required DateTime ArrivalTime { get; set; }

    [Required]
    public required string AircraftType { get; set; }

    [Required]
    public required string AircraftNumber { get; set; }

    [Required]
    public required string PilotName { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int PassengerCapacity { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int BusinessClassCapacity { get; set; }

    public static ValidationResult ValidateFlightTimes(DateTime arrivalTime, ValidationContext context)
    {
        var instance = (Flight)context.ObjectInstance;
        if (arrivalTime < instance.DepartureTime)
        {
            return new ValidationResult("Arrival time must be after departure time.");
        }
        return ValidationResult.Success;
    }
}