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
    /// Displays the error page.
    /// </summary>
    /// <returns>The error view.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

/// <summary>
/// ViewModel for the home page statistics.
/// </summary>
public class HomeViewModel
{
    public int TotalFlights { get; set; }
    public int TotalCapacity { get; set; }
    public List<Flight>? UpcomingFlights { get; set; }
}