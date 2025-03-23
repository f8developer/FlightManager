using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models;

public class ReservationUser
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public required string UserName { get; set; }

    [Required, StringLength(50)]
    public required string FirstName { get; set; }

    [Required, StringLength(50)]
    public required string MiddleName { get; set; }

    [Required, StringLength(50)]
    public required string LastName { get; set; }

    [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "EGN must be 10 digits.")]
    public required string EGN { get; set; }

    [Required]
    public required string Address { get; set; }

    [Required]
    public required string PhoneNumber { get; set; }

    public ICollection<Reservation>? Reservations { get; set; }

    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
