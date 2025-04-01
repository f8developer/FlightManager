using Microsoft.AspNetCore.Identity;

namespace FlightManager.Data.Models;

/// <summary>
/// Represents an application user that extends the IdentityUser class.
/// </summary>
public class AppUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the collection of reservation users associated with this user.
    /// </summary>
    public virtual ICollection<ReservationUser>? ReservationUsers { get; set; }
}