using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models;

public class AppUser : IdentityUser
{
   
    [Required, StringLength(50)]
    public required string Username { get; set; }

    [Required, StringLength(50)]
    public required string FirstName { get; set; }

    [StringLength(50)]
    public string? MiddleName { get; set; }  

    [Required, StringLength(50)]
    public required string LastName { get; set; }

    [Required, StringLength(10, MinimumLength = 10)]
    public required string EGN { get; set; }

    [Required]
    public required string Address { get; set; }
}