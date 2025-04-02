using FlightManager.Data;
using FlightManager.Data.Models;
using FlightManager.EmailService;
using FlightManager.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FlightManager;

/// <summary>
/// The main entry point for the FlightManager application.
/// </summary>
public class Program
{
    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the default connection string is not found.</exception>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Configure email service
        builder.Services.AddTransient<BrevoEmailService>();

        // Bind the configuration section to a strongly-typed object
        builder.Services.Configure<OwnerSettings>(builder.Configuration.GetSection("OwnerSettings"));

        // Add singleton so Razor views can access it
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OwnerSettings>>().Value);

        builder.Services.AddDefaultIdentity<AppUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 3;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddControllersWithViews();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            // Disable redirects for access denied
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = builder.Environment.IsDevelopment()
                    ? StatusCodes.Status403Forbidden  // 403 in development
                    : StatusCodes.Status404NotFound;  // 404 in production
                return Task.CompletedTask;
            };
        });

        var app = builder.Build();

        OwnerSettings ownerSettings = builder.Configuration.GetSection("OwnerSettings").Get<OwnerSettings>();

        await AppStart.InitializeAsync(app, ownerSettings);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        app.Run();
    }
}

/// <summary>
/// Represents configuration settings for the owner user.
/// </summary>
public class OwnerSettings
{
    /// <summary>
    /// Gets or sets the email address for the owner user.
    /// </summary>
    public string OwnerEmail { get; set; }

    /// <summary>
    /// Gets or sets the password for the owner user.
    /// </summary>
    public string OwnerPassword { get; set; }
}