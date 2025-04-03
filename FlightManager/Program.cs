using FlightManager.Data;
using FlightManager.Data.Models;
using FlightManager.EmailService;
using FlightManager.Extensions;
using FlightManager.Extensions.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace FlightManager;

/// <summary>
/// The main entry point for the FlightManager application.
/// </summary>
public class Program
{
    /// <summary>
    /// The entry point of the application.
    /// </summary>
    public static async Task Main(string[] args)
    {
        // Set default culture for all threads
        var cultureInfo = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        var builder = WebApplication.CreateBuilder(args);

        // Configure request localization
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US");
            options.SupportedCultures = new[] { cultureInfo };
            options.SupportedUICultures = new[] { cultureInfo };
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
        });

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Configure email service
        builder.Services.AddTransient<BrevoEmailService>();
        builder.Services.AddScoped<EmailTemplateService>();

        // Bind configuration sections
        builder.Services.Configure<OwnerSettings>(builder.Configuration.GetSection("OwnerSettings"));
        builder.Services.Configure<ReservationCleanupSettings>(
            builder.Configuration.GetSection("ReservationCleanup"));

        // Add singleton for Razor views
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OwnerSettings>>().Value);

        // Add reservation cleanup service
        builder.Services.AddHostedService<ReservationCleanupService>();
        builder.Services.AddSingleton<ReservationCleanupService>();

        // Configure identity with relaxed password requirements
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

        // Configure controllers with JSON options
        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase
            });

        // Configure application cookie
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = builder.Environment.IsDevelopment()
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            };
        });

        var app = builder.Build();

        // Use request localization
        app.UseRequestLocalization();

        // Initialize owner settings
        var ownerSettings = builder.Configuration.GetSection("OwnerSettings").Get<OwnerSettings>();
        await AppStart.InitializeAsync(app, ownerSettings);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
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
    public string OwnerEmail { get; set; }
    public string OwnerPassword { get; set; }
}

public class ReservationCleanupSettings
{
    public int CheckIntervalMinutes { get; set; } = 10;
    public int ExpiryHours { get; set; } = 48;
}