using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightManager.Models;

public class Reservation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required int ReservationUserId { get; set; }

    public ReservationUser ReservationUser { get; set; }

    [Required]
    public int FlightId { get; set; }

    public Flight Flight { get; set; }

    [Required, StringLength(50)]
    public required string Nationality { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(12)")] // Store enum as string in the database
    public TicketType TicketType { get; set; } // Now uses the enum
}