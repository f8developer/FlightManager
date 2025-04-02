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
    /// Displays a list of all flights.
    /// </summary>
    /// <returns>The flights list view.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var flights = await _context.Flights
            .Include(f => f.Reservations)
            .ToListAsync();

        return View(flights);
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
    /// <returns>The flight creation view.</returns>
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View(); // Ensure a new instance is passed
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
    /// Displays a list of passengers for a specific flight.
    /// </summary>
    /// <param name="id">The flight ID.</param>
    /// <returns>The passengers list view or NotFound if flight doesn't exist.</returns>
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Passengers(int id)
    {
        var flight = await _context.Flights
            .Include(f => f.Reservations)
                .ThenInclude(r => r.ReservationUser)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flight == null)
        {
            return NotFound();
        }

        return View(flight);
    }

    private bool FlightExists(int id)
    {
        return _context.Flights.Any(e => e.Id == id);
    }
}