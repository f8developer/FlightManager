using Microsoft.AspNetCore.Identity;

namespace FlightManager.Models;

public class AppUser : IdentityUser
{
    public virtual ICollection<ReservationUser>? ReservationUsers { get; set; }
}