using System.ComponentModel.DataAnnotations;

namespace FlightManager.Data.Models;

/// <summary>
/// ViewModel for creating and managing group flight reservations containing multiple passengers.
/// </summary>
public class GroupReservationViewModel
{
    /// <summary>
    /// Gets or sets the ID of the flight being reserved.
    /// </summary>
    public int FlightId { get; set; }

    /// <summary>
    /// Gets or sets the nationality of the passengers in the group.
    /// </summary>
    public string Nationality { get; set; }

    /// <summary>
    /// Gets or sets the type of ticket (Business/Economy) for the group reservation.
    /// </summary>
    public TicketType TicketType { get; set; }

    /// <summary>
    /// Gets or sets the email address for the group reservation contact.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the list of passengers included in this group reservation.
    /// Initialized with an empty list by default.
    /// </summary>
    public List<PassengerViewModel> Passengers { get; set; } = new List<PassengerViewModel>();

    /// <summary>
    /// Gets or sets whether the group reservation has been confirmed.
    /// Defaults to false when creating a new reservation.
    /// </summary>
    public bool IsConfirmed { get; set; } = false;

    /// <summary>
    /// Gets or sets the unique token used for confirming the reservation.
    /// Initialized with a new GUID by default.
    /// </summary>
    public string ConfirmationToken { get; set; } = Guid.NewGuid().ToString("N");
}

/// <summary>
/// ViewModel representing an individual passenger in a group reservation.
/// </summary>
public class PassengerViewModel
{
    /// <summary>
    /// Gets or sets the username for the passenger.
    /// </summary>
    [Required]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the passenger's first name.
    /// </summary>
    [Required]
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the passenger's middle name (optional).
    /// </summary>
    public string MiddleName { get; set; }

    /// <summary>
    /// Gets or sets the passenger's last name.
    /// </summary>
    [Required]
    public string LastName { get; set; }

    /// <summary>
    /// Gets or sets the passenger's EGN (Bulgarian personal identification number).
    /// Must be exactly 10 digits.
    /// </summary>
    [Required]
    public string EGN { get; set; }

    /// <summary>
    /// Gets or sets the passenger's address.
    /// </summary>
    [Required]
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the passenger's phone number.
    /// </summary>
    [Required]
    public string PhoneNumber { get; set; }
}