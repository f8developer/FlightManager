using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FlightManager.Controllers;

/// <summary>
/// Controller for handling general application views.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeController"/> class.
    /// </summary>
    /// <param name="logger">The logger service.</param>
    /// <param name="context">The database context.</param>
    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Displays the home page with flight statistics.
    /// </summary>
    /// <returns>The home page view.</returns>
    public async Task<IActionResult> Index()
    {
        var flightStats = new HomeViewModel
        {
            TotalFlights = await _context.Flights.CountAsync(),
            TotalCapacity = await _context.Flights.SumAsync(f => f.PassengerCapacity),
            UpcomingFlights = await _context.Flights
                .Where(f => f.DepartureTime > DateTime.Now)
                .OrderBy(f => f.DepartureTime)
                .Take(5)
                .ToListAsync()
        };

        return View(flightStats);
    }

    /// <summary>
    /// Displays the privacy policy page.
    /// </summary>
    /// <returns>The privacy policy view.</returns>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Displays the error page with diagnostic information.
    /// The response is not cached to ensure accurate error reporting.
    /// </summary>
    /// <returns>An error view containing the current request ID for troubleshooting.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

/// <summary>
/// Represents the data model for the home page view, containing flight statistics and upcoming flights.
/// </summary>
public class HomeViewModel
{
    /// <summary>
    /// Gets or sets the total number of flights in the system.
    /// </summary>
    public int TotalFlights { get; set; }

    /// <summary>
    /// Gets or sets the sum of all passenger capacities across all flights.
    /// </summary>
    public int TotalCapacity { get; set; }

    /// <summary>
    /// Gets or sets the list of upcoming flights (next 5 by departure time).
    /// </summary>
    public List<Flight>? UpcomingFlights { get; set; }
}