using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Controllers;

/// <summary>
/// Controller for managing flight operations.
/// </summary>
[Authorize(Roles = "Admin,Employee")]
public class FlightsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlightsController"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="env">The web hosting environment.</param>
    public FlightsController(
        ApplicationDbContext context,
        IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Displays a paginated list of flights with optional filtering by location and date range.
    /// Applies smart date matching when both departure and arrival dates are provided.
    /// </summary>
    /// <param name="fromLocation">Optional filter for departure location.</param>
    /// <param name="toLocation">Optional filter for arrival location.</param>
    /// <param name="departureDate">Optional filter for departure date.</param>
    /// <param name="arrivalDate">Optional filter for arrival date.</param>
    /// <param name="pageNumber">Current page number for pagination.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A view containing the filtered and paginated flight list.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string fromLocation,
        string toLocation,
        string departureDate,
        string arrivalDate,
        int? pageNumber,
        int? pageSize)
    {
        // Set default page size if not specified
        int currentPageSize = pageSize ?? 10;
        int currentPageNumber = pageNumber ?? 1;

        ViewBag.CurrentPageSize = currentPageSize;
        ViewBag.AvailablePageSizes = new List<int> { 5, 10, 20, 50 };

        // Start with all flights including reservations
        var flightsQuery = _context.Flights
            .Include(f => f.Reservations)
            .AsQueryable();

        // Apply location filters
        if (!string.IsNullOrEmpty(fromLocation))
        {
            flightsQuery = flightsQuery.Where(f => f.FromLocation.Contains(fromLocation));
        }

        if (!string.IsNullOrEmpty(toLocation))
        {
            flightsQuery = flightsQuery.Where(f => f.ToLocation.Contains(toLocation));
        }

        DateTime? startDate = null;
        DateTime? endDate = null;

        if (!string.IsNullOrEmpty(departureDate) && DateTime.TryParse(departureDate, out var parsedStartDate))
        {
            startDate = parsedStartDate.Date;
        }

        if (!string.IsNullOrEmpty(arrivalDate) && DateTime.TryParse(arrivalDate, out var parsedEndDate))
        {
            endDate = parsedEndDate.Date;
        }

        PaginatedList<Flight> paginatedFlights;

        if (startDate.HasValue || endDate.HasValue)
        {
            // Filter flights that overlap with the selected date range
            flightsQuery = flightsQuery.Where(f =>
                (!startDate.HasValue || f.DepartureTime.Date <= endDate) &&
                (!endDate.HasValue || f.ArrivalTime.Date >= startDate));

            // Get filtered flights
            var filteredFlights = await flightsQuery.AsNoTracking().ToListAsync();

            // Calculate relevance scores for ordering
            var scoredFlights = filteredFlights
                .Select(f => new
                {
                    Flight = f,
                    DepartureScore = startDate.HasValue
                        ? Math.Abs((f.DepartureTime.Date - startDate.Value).TotalDays)
                        : 0,
                    ArrivalScore = endDate.HasValue
                        ? Math.Abs((f.ArrivalTime.Date - endDate.Value).TotalDays)
                        : 0,
                    // Additional score for flights that fully fit within the range
                    FullMatchBonus = (startDate.HasValue && endDate.HasValue &&
                                    f.DepartureTime.Date >= startDate.Value &&
                                    f.ArrivalTime.Date <= endDate.Value) ? -1000 : 0
                })
                .OrderBy(x => x.FullMatchBonus + x.DepartureScore + x.ArrivalScore)
                .Select(x => x.Flight);

            // Create paginated list
            paginatedFlights = PaginatedList<Flight>.CreateFromEnumerable(
                scoredFlights,
                currentPageNumber,
                currentPageSize,
                filteredFlights.Count);
        }
        else
        {
            // Default ordering by departure time if no dates are provided
            flightsQuery = flightsQuery.OrderBy(f => f.DepartureTime);
            paginatedFlights = await PaginatedList<Flight>.CreateAsync(
                flightsQuery.AsNoTracking(),
                currentPageNumber,
                currentPageSize);
        }

        return View(paginatedFlights);
    }

    /// <summary>
    /// Provides location suggestions for autocomplete functionality in flight search forms.
    /// </summary>
    /// <param name="term">The search term entered by the user.</param>
    /// <param name="isDeparture">True if searching for departure locations, false for arrival locations.</param>
    /// <returns>A JSON list of matching location suggestions.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> GetLocationSuggestions(string term, bool isDeparture)
    {
        var locations = await _context.Flights
            .Where(f => isDeparture
                ? f.FromLocation.Contains(term)
                : f.ToLocation.Contains(term))
            .Select(f => isDeparture ? f.FromLocation : f.ToLocation)
            .Distinct()
            .Take(10)
            .ToListAsync();

        return Json(locations);
    }

    /// <summary>
    /// Displays details for a specific flight.
    /// </summary>
    /// <param name="id">The flight ID.</param>
    /// <returns>The flight details view or NotFound if flight doesn't exist.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var flight = await _context.Flights
            .Include(f => f.Reservations)
                .ThenInclude(r => r.ReservationUser)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (flight == null)
        {
            return NotFound();
        }

        return View(flight);
    }

    /// <summary>
    /// Displays the flight creation form.
    /// </summary>
    /// <returns>The flight creation view with an empty flight model.</returns>
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        // Initialize with default values
        var flight = new Flight
        {
            FromLocation = string.Empty,
            ToLocation = string.Empty,
            AircraftType = string.Empty,
            AircraftNumber = string.Empty,
            PilotName = string.Empty,
            DepartureTime = DateTime.UtcNow.AddHours(1), // Default to 1 hour from now
            ArrivalTime = DateTime.UtcNow.AddHours(2),   // Default to 2 hours from now
            PassengerCapacity = 100,                  // Default capacity
            BusinessClassCapacity = 20                // Default business class capacity
        };

        return View(flight);
    }

    /// <summary>
    /// Handles flight creation form submission.
    /// </summary>
    /// <param name="flight">The flight data to create.</param>
    /// <returns>Redirects to flight list on success or returns creation view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Flight flight)
    {
        // Clear existing model state for custom validated fields
        ModelState.Remove(nameof(Flight.ArrivalTime));
        ModelState.Remove(nameof(Flight.BusinessClassCapacity));

        // Custom validation
        if (flight.ArrivalTime <= flight.DepartureTime)
        {
            ModelState.AddModelError(nameof(Flight.ArrivalTime), "Arrival time must be after departure time");
        }

        if (flight.PassengerCapacity <= 0)
        {
            ModelState.AddModelError(nameof(Flight.PassengerCapacity), "Passenger capacity must be at least 1");
        }

        if (flight.BusinessClassCapacity > flight.PassengerCapacity)
        {
            ModelState.AddModelError(nameof(Flight.BusinessClassCapacity), "Business class capacity cannot exceed total passenger capacity");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(flight);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Flight created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Unable to save changes. Please try again.");
                if (_env.IsDevelopment())
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
        }

        // For development debugging
        if (_env.IsDevelopment())
        {
            ViewData["ModelErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new
                {
                    Field = x.Key,
                    Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                })
                .ToList();
        }

        return View(flight);
    }

    /// <summary>
    /// Displays the flight edit form.
    /// </summary>
    /// <param name="id">The flight ID to edit.</param>
    /// <returns>The edit view or NotFound if flight doesn't exist.</returns>
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var flight = await _context.Flights.FindAsync(id);
        if (flight == null)
        {
            return NotFound();
        }
        return View(flight);
    }

    /// <summary>
    /// Handles flight edit form submission.
    /// </summary>
    /// <param name="id">The flight ID being edited.</param>
    /// <param name="flight">The updated flight data.</param>
    /// <returns>Redirects to flight list on success or returns edit view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Flight flight)
    {
        if (id != flight.Id)
        {
            return NotFound();
        }

        // Clear existing model state for custom validated fields
        ModelState.Remove(nameof(Flight.ArrivalTime));
        ModelState.Remove(nameof(Flight.BusinessClassCapacity));

        // Custom validation
        if (flight.ArrivalTime <= flight.DepartureTime)
        {
            ModelState.AddModelError(nameof(Flight.ArrivalTime), "Arrival time must be after departure time.");
        }

        if (flight.PassengerCapacity <= 0)
        {
            ModelState.AddModelError(nameof(Flight.PassengerCapacity), "Passenger capacity must be at least 1.");
        }

        if (flight.BusinessClassCapacity > flight.PassengerCapacity)
        {
            ModelState.AddModelError(nameof(Flight.BusinessClassCapacity), "Business class capacity cannot exceed total passenger capacity.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(flight);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Flight updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlightExists(flight.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // For development debugging
        if (_env.IsDevelopment())
        {
            ViewData["ModelErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new
                {
                    Field = x.Key,
                    Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                })
                .ToList();
        }

        // Return view with the flight data and validation errors
        return View(flight);
    }

    /// <summary>
    /// Displays the flight deletion confirmation form.
    /// </summary>
    /// <param name="id">The flight ID to delete.</param>
    /// <returns>The deletion confirmation view or NotFound if flight doesn't exist.</returns>
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var flight = await _context.Flights
            .Include(f => f.Reservations)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (flight == null)
        {
            return NotFound();
        }

        if (flight.Reservations.Any())
        {
            ViewBag.HasReservations = true;
            ViewBag.ReservationCount = flight.Reservations.Count;
        }

        return View(flight);
    }

    /// <summary>
    /// Handles flight deletion confirmation.
    /// </summary>
    /// <param name="id">The flight ID to delete.</param>
    /// <returns>Redirects to flight list.</returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var flight = await _context.Flights
            .Include(f => f.Reservations)
                .ThenInclude(r => r.ReservationUser) // Include ReservationUsers
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flight != null)
        {
            // Get all ReservationUsers that are ONLY associated with this flight
            var usersToDelete = flight.Reservations
                .Select(r => r.ReservationUser)
                .Where(u =>
                    // Only delete users that don't have other reservations
                    !_context.Reservations.Any(r =>
                        r.ReservationUserId == u.Id &&
                        r.FlightId != flight.Id) &&
                    // And aren't linked to an AppUser
                    u.AppUserId == null)
                .Distinct()
                .ToList();

            // First delete all reservations
            _context.Reservations.RemoveRange(flight.Reservations);

            // Then delete the associated ReservationUsers (if they meet criteria)
            _context.ReservationUsers.RemoveRange(usersToDelete);

            // Finally delete the flight
            _context.Flights.Remove(flight);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Flight and {flight.Reservations.Count} associated reservation(s) deleted successfully. " +
                                       $"{usersToDelete.Count} unused passenger record(s) were also removed.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays a paginated list of passengers for a specific flight, sorted by ticket type and confirmation status.
    /// </summary>
    /// <param name="id">The ID of the flight to view passengers for.</param>
    /// <param name="pageNumber">Current page number for pagination (default: 1).</param>
    /// <param name="pageSize">Number of passengers per page (default: 5).</param>
    /// <returns>A view containing flight details and paginated passenger list.</returns>
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Passengers(int id, int pageNumber = 1, int pageSize = 5)
    {
        var flight = await _context.Flights
            .Include(f => f.Reservations)
                .ThenInclude(r => r.ReservationUser)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flight == null)
        {
            return NotFound();
        }

        // Convert to list first since we're working with in-memory collections
        var reservations = flight.Reservations
            .OrderBy(r => r.TicketType)
            .ThenBy(r => r.IsConfirmed)
            .ThenBy(r => r.ReservationUser.UserName)
            .ToList();

        // Create the paginated list
        var paginatedList = PaginatedList<Reservation>.CreateFromEnumerable(
            reservations,
            pageNumber,
            pageSize,
            reservations.Count);

        // Create a view model that contains both the flight and paginated reservations
        var viewModel = new FlightPassengersViewModel
        {
            Flight = flight,
            PaginatedReservations  = paginatedList
        };

        ViewBag.AvailablePageSizes = new[] { 5, 10, 25, 50, 100 };

        return View(viewModel);
    }

    /// <summary>
    /// Checks if a flight with the specified ID exists in the database.
    /// </summary>
    /// <param name="id">The ID of the flight to check.</param>
    /// <returns>True if the flight exists, false otherwise.</returns>
    private bool FlightExists(int id)
    {
        return _context.Flights.Any(e => e.Id == id);
    }
}