using System.ComponentModel.DataAnnotations;

namespace FlightManager.Data.Models;

/// <summary>
/// Represents a user who makes flight reservations.
/// </summary>
public class ReservationUser
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
    [Display(Name = "Username")]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    [Display(Name = "First Name")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Middle name is required.")]
    [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters.")]
    [Display(Name = "Middle Name")]
    public required string MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    [Display(Name = "Last Name")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "EGN is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "EGN must be exactly 10 digits.")]
    [Display(Name = "EGN (Personal ID)")]
    public required string EGN { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(100, ErrorMessage = "Address cannot exceed 100 characters.")]
    [Display(Name = "Address")]
    public required string Address { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$",
        ErrorMessage = "Invalid phone number format.")]
    [Display(Name = "Phone Number")]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the collection of reservations made by this user.
    /// </summary>
    public ICollection<Reservation>? Reservations { get; set; }

    /// <summary>
    /// Gets or sets the associated application user ID.
    /// </summary>
    public string? AppUserId { get; set; }

    /// <summary>
    /// Gets or sets the associated application user.
    /// </summary>
    public AppUser? AppUser { get; set; }
}