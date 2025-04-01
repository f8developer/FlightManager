using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Extensions.AppStartLogic;

/// <summary>
/// Contains logic for seeding initial roles and the owner user during application startup.
/// </summary>
internal class SeedRolesAndOwner
{
    /// <summary>
    /// Seeds the database with required roles and creates the owner user if they don't exist.
    /// </summary>
    /// <param name="scope">The IServiceScope containing required services.</param>
    /// <param name="ownerSettings">Configuration settings for the owner user.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when owner password is not configured.</exception>
    /// <exception cref="Exception">Thrown when owner user creation fails.</exception>
    internal static async Task SeedRolesAndOwnerAsync(IServiceScope scope, OwnerSettings ownerSettings)
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            // Apply pending migrations
            await context.Database.MigrateAsync();

            // Create roles if they don't exist
            string[] roles = { "Admin", "Owner" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var ownerEmail = ownerSettings.OwnerEmail;

            // Setup owner user
            var ownerUser = new AppUser
            {
                UserName = ownerEmail,
                Email = ownerEmail
            };

            // Create admin user if no users exist
            if (await userManager.FindByEmailAsync(ownerUser.Email.ToString()) == null)
            {
                var ownerPassword = ownerSettings.OwnerPassword ??
                                    configuration["OwnerPassword"] ??
                                    Environment.GetEnvironmentVariable("OwnerPassword");
                if (string.IsNullOrEmpty(ownerPassword))
                {
                    throw new InvalidOperationException("Owner password not configured in secrets.");
                }
                // Create the user with a secure password
                var result = await userManager.CreateAsync(ownerUser, ownerPassword);
                if (!result.Succeeded)
                {
                    var logger = services.GetRequiredService<ILogger<SeedRolesAndOwner>>();
                    logger.LogError("Failed to create Owner user. Errors: {Errors}", string.Join(", ", result.Errors));
                    throw new Exception("Failed to create Owner user.");
                }

                if (result.Succeeded)
                {
                    // Assign both roles to the admin user
                    await userManager.AddToRolesAsync(ownerUser, roles);
                }
                else
                {
                    Console.WriteLine("Failed to create admin user: {Errors}", result.Errors);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while seeding the database. -> {ex}");
        }
    }
}