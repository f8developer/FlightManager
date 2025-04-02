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
    /// Seeds the database with required roles and ensures the owner user exists with the latest password.
    /// </summary>
    /// <param name="scope">The IServiceScope containing required services.</param>
    /// <param name="ownerSettings">Configuration settings for the owner user.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when owner password is not configured.</exception>
    /// <exception cref="Exception">Thrown when owner user creation or update fails.</exception>
    internal static async Task SeedRolesAndOwnerAsync(IServiceScope scope, OwnerSettings ownerSettings)
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<SeedRolesAndOwner>>();

            // Apply pending migrations
            await context.Database.MigrateAsync();

            // Create roles if they don't exist
            string[] roles = { "Employee", "Admin", "Owner" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var ownerEmail = ownerSettings.OwnerEmail;
            var ownerPassword = ownerSettings.OwnerPassword ??
                                configuration["OwnerPassword"] ??
                                Environment.GetEnvironmentVariable("OwnerPassword");

            if (string.IsNullOrEmpty(ownerPassword))
            {
                throw new InvalidOperationException("Owner password not configured in secrets.");
            }

            // Check if owner exists
            var ownerUser = await userManager.FindByEmailAsync(ownerEmail);

            if (ownerUser == null)
            {
                // Create new owner user if not found
                ownerUser = new AppUser
                {
                    UserName = ownerEmail,
                    Email = ownerEmail
                };

                var createResult = await userManager.CreateAsync(ownerUser, ownerPassword);
                if (!createResult.Succeeded)
                {
                    logger.LogError("Failed to create Owner user. Errors: {Errors}", string.Join(", ", createResult.Errors));
                    throw new Exception("Failed to create Owner user.");
                }

                // Assign roles to owner
                await userManager.AddToRolesAsync(ownerUser, roles);
            }
            else
            {
                // Ensure the owner has all required roles
                foreach (var role in roles)
                {
                    if (!await userManager.IsInRoleAsync(ownerUser, role))
                    {
                        await userManager.AddToRoleAsync(ownerUser, role);
                    }
                }

                // Update the owner's password to the latest one
                var passwordHash = userManager.PasswordHasher.HashPassword(ownerUser, ownerPassword);
                ownerUser.PasswordHash = passwordHash;
                await userManager.UpdateAsync(ownerUser);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while seeding the database. -> {ex}");
        }
    }
}
