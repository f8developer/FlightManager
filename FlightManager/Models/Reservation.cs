using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models;

public class Reservation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required AppUser User { get; set; }

    [Required]
    public required Flight Flight { get; set; }

    [Required]
    public required string UserId { get; set; }

    [Required]
    public int FlightId { get; set; }

    [Required, StringLength(50)]
    public required string Nationality { get; set; }

    [Required]
    public required string TicketType { get; set; } // Business or Economy
}