// Add this to your Models folder
using System.ComponentModel.DataAnnotations;

namespace FlightManager.Data.Models;

public class GroupReservationViewModel
{
    public int FlightId { get; set; }
    public string Nationality { get; set; }
    public TicketType TicketType { get; set; }
    public string Email { get; set; }
    public List<PassengerViewModel> Passengers { get; set; } = new List<PassengerViewModel>();
    public bool IsConfirmed { get; set; } = false;
    public string ConfirmationToken { get; set; } = Guid.NewGuid().ToString("N");
}

public class PassengerViewModel
{
    [Required]
    public string UserName { get; set; }

    [Required]
    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    public string EGN { get; set; }

    [Required]
    public string Address { get; set; }

    [Required]
    public string PhoneNumber { get; set; }
}