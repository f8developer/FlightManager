using FlightManager.Extensions.AppStartLogic;

namespace FlightManager.Extensions
{
    public class AppStart
    {
        public static async Task InitializeAsync(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                await SeedRolesAndOwner.SeedRolesAndOwnerAsync(scope);
            }
        }
    }
}
