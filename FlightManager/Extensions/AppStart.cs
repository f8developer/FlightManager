using FlightManager.Extensions.AppStartLogic;

namespace FlightManager.Extensions
{
    /// <summary>
    /// Provides application startup initialization logic.
    /// </summary>
    public class AppStart
    {
        /// <summary>
        /// Initializes the application by seeding required roles and owner user asynchronously.
        /// </summary>
        /// <param name="app">The WebApplication instance to initialize.</param>
        /// <param name="ownerSettings">Configuration settings for the owner user.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public static async Task InitializeAsync(WebApplication app, OwnerSettings ownerSettings)
        {
            using (var scope = app.Services.CreateScope())
            {
                await SeedRolesAndOwner.SeedRolesAndOwnerAsync(scope, ownerSettings);
            }
        }
    }
}