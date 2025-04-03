using FlightManager.Data;
using FlightManager.Data.Models;
using FlightManager.EmailService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using FlightManager.Extensions;
using FlightManager.Extensions.Services;

namespace FlightManager.Controllers;

/// <summary>
/// Controller for managing flight reservations.
/// </summary>
[Authorize(Roles = "Admin")]
public class ReservationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    public UserManager<AppUser> _userManager;
    private readonly BrevoEmailService _emailService;
    private readonly EmailTemplateService _templateService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationsController"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="env">The web hosting environment.</param>
    /// <param name="userManager">The user manager service.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="templateService">The email template service.</param>
    public ReservationsController(
        ApplicationDbContext context,
        IWebHostEnvironment env,
        UserManager<AppUser> userManager,
        BrevoEmailService emailService,
        EmailTemplateService templateService)
    {
        _context = context;
        _env = env;
        _userManager = userManager;
        _emailService = emailService;
        _templateService = templateService;
    }

    /// <summary>
    /// Displays a list of reservations with optional filtering.
    /// </summary>
    /// <param name="id">User ID filter.</param>
    /// <param name="username">Username filter.</param>
    /// <param name="firstName">First name filter.</param>
    /// <param name="lastName">Last name filter.</param>
    /// <returns>The reservations list view.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string id,
        int? pageNumber,
        int? pageSize)
    {
        // Set default page size if not specified
        int currentPageSize = pageSize ?? 10;
        int currentPageNumber = pageNumber ?? 1;

        ViewBag.CurrentPageSize = currentPageSize;
        ViewBag.AvailablePageSizes = new List<int> { 5, 10, 20, 50 };
        ViewBag.SearchId = id;
        ViewBag.HasSearched = !string.IsNullOrEmpty(id);

        var reservationsQuery = _context.Reservations
            .Include(r => r.Flight)
            .Include(r => r.ReservationUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(id))
        {
            reservationsQuery = reservationsQuery.Where(r => r.Id.ToString() == id);
        }

        var paginatedReservations = await PaginatedList<Reservation>.CreateAsync(
            reservationsQuery.OrderByDescending(r => r.CreatedAt),
            currentPageNumber,
            currentPageSize);

        return View(paginatedReservations);
    }

    /// <summary>
    /// Displays details for a specific reservation.
    /// </summary>
    /// <param name="id">The reservation ID.</param>
    /// <returns>The reservation details view or NotFound.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var reservation = await _context.Reservations
            .Include(r => r.Flight)
            .Include(r => r.ReservationUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        return reservation == null ? NotFound() : View(reservation);
    }

    /// <summary>
    /// Displays the reservation creation form.
    /// </summary>
    /// <returns>The reservation creation view.</returns>
    [AllowAnonymous]
    public IActionResult Create(string email = "")
    {
        // Get all flights with their reservations
        var flightsWithReservations = _context.Flights
            .Include(f => f.Reservations)
            .ToList();

        // Filter out fully booked flights
        var availableFlights = flightsWithReservations
            .Where(f =>
            {
                var businessReserved = f.Reservations.Count(r => r.TicketType == TicketType.Business);
                var standardReserved = f.Reservations.Count(r => r.TicketType != TicketType.Business);
                var standardCapacity = f.PassengerCapacity - f.BusinessClassCapacity;

                return businessReserved < f.BusinessClassCapacity ||
                       standardReserved < standardCapacity;
            })
            .ToList();

        if (!availableFlights.Any())
        {
            ViewBag.NoFlightsAvailable = "Currently there are no flights with available seats.";
        }

        // Create a list of SelectListItems with detailed flight information
        var flightList = availableFlights.Select(f => new SelectListItem
        {
            Value = f.Id.ToString(),
            Text = $"{f.AircraftNumber} - {f.FromLocation} → {f.ToLocation} ({f.DepartureTime.ToString("g")} - {f.ArrivalTime.ToString("g")})"
        }).ToList();

        // Pass the flight list to the ViewData to populate the dropdown in the view
        ViewData["FlightId"] = new SelectList(flightList, "Value", "Text");
        ViewBag.FlightList = flightList;
        ViewData["Email"] = email;

        return View();
    }

    /// <summary>
    /// Handles reservation creation form submission.
    /// </summary>
    /// <param name="reservation">The reservation data to create.</param>
    /// <returns>Redirects to details view on success or returns creation view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        [Bind("FlightId,Nationality,TicketType,ReservationUser")] Reservation reservation,
        string email)
    {
        ModelState.Remove("Flight");
        ModelState.Remove("ReservationUser.Reservations");
        ModelState.Remove("ReservationUserId");

        if (!string.IsNullOrWhiteSpace(email) && !new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError("", "Please enter a valid email address");
        }
        else
        {
            reservation.ReservationUser.Email = email;
        }

        // Get the flight information
        var flight = await _context.Flights
            .Include(f => f.Reservations)
            .FirstOrDefaultAsync(f => f.Id == reservation.FlightId);

        if (flight == null)
        {
            ModelState.AddModelError("", "Selected flight does not exist.");
        }
        else
        {
            // Check seat availability based on ticket type
            if (reservation.TicketType == TicketType.Business)
            {
                var businessReserved = flight.Reservations.Count(r => r.TicketType == TicketType.Business);
                if (businessReserved >= flight.BusinessClassCapacity)
                {
                    ModelState.AddModelError("", "No available seats in business class for this flight.");
                }
            }
            else
            {
                var standardReserved = flight.Reservations.Count(r => r.TicketType != TicketType.Business);
                var standardCapacity = flight.PassengerCapacity - flight.BusinessClassCapacity;
                if (standardReserved >= standardCapacity)
                {
                    ModelState.AddModelError("", "No available seats in economy class for this flight.");
                }
            }
        }

        // Manual validation for existing reservation
        if (reservation.ReservationUser != null)
        {
            var existingUser = await _context.ReservationUsers
                .FirstOrDefaultAsync(u => u.EGN == reservation.ReservationUser.EGN);

            if (existingUser != null)
            {
                bool hasExistingReservation = await _context.Reservations
                    .AnyAsync(r => r.FlightId == reservation.FlightId &&
                                  r.ReservationUserId == existingUser.Id);

                if (hasExistingReservation)
                {
                    ModelState.AddModelError("", "This user already has a reservation for this flight.");
                }
            }
        }

        if (ModelState.IsValid)
        {
            // Handle existing user or create new one
            var existingUser = await _context.ReservationUsers
                .FirstOrDefaultAsync(u => u.EGN == reservation.ReservationUser.EGN);

            if (existingUser != null)
            {
                reservation.ReservationUserId = existingUser.Id;
                reservation.ReservationUser = null;
            }
            else
            {
                _context.ReservationUsers.Add(reservation.ReservationUser);
                await _context.SaveChangesAsync();
                reservation.ReservationUserId = reservation.ReservationUser.Id;
            }

            reservation.IsConfirmed = false;
            reservation.ConfirmationToken = Guid.NewGuid().ToString("N");
            reservation.CreatedAt = DateTime.UtcNow;

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Generate confirmation URL
            var confirmationRouteValues = new { id = reservation.Id, token = reservation.ConfirmationToken };
            var confirmationUrl = Url.Action(
                action: "ConfirmReservation",
                controller: "Reservations",
                values: confirmationRouteValues,
                protocol: Request.Scheme);
            var detailsUrl = Url.Action(
                action: "Details",
                controller: "Reservations",
                values: new { id = reservation.Id },
                protocol: Request.Scheme);

            // Send email notification if requested and email is valid
            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    var user = existingUser ?? reservation.ReservationUser;

                    if (user != null && flight != null)
                    {
                        var emailContent = GenerateReservationEmail.ReservationEmail(
                            reservation,
                            flight,
                            user,
                            confirmationUrl,
                            detailsUrl,
                            _templateService);

                        _emailService.SendEmail(
                            subject: $"Your Flight Reservation Confirmation #{reservation.Id}",
                            htmlContent: emailContent,
                            recipientEmail: email,
                            recipientName: user.UserName
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't prevent reservation creation
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }
            }

            // Return to the form with success message
            TempData["SuccessMessage"] = "Reservation created successfully! Check your email for confirmation.";
            return RedirectToAction("Create", new { email = email });
        }

        // Repopulate flight list if validation fails
        var flightsWithReservations = _context.Flights
            .Include(f => f.Reservations)
            .ToList();

        var availableFlights = flightsWithReservations
            .Where(f =>
            {
                var businessReserved = f.Reservations.Count(r => r.TicketType == TicketType.Business);
                var standardReserved = f.Reservations.Count(r => r.TicketType != TicketType.Business);
                var standardCapacity = f.PassengerCapacity - f.BusinessClassCapacity;

                return businessReserved < f.BusinessClassCapacity ||
                       standardReserved < standardCapacity;
            })
            .ToList();

        if (!availableFlights.Any())
        {
            ViewBag.NoFlightsAvailable = "Currently there are no flights with available seats.";
        }

        ViewData["FlightId"] = new SelectList(
            availableFlights.Select(f => new
            {
                f.Id,
                DisplayText = $"{f.AircraftNumber} - {f.FromLocation} → {f.ToLocation} ({f.DepartureTime:g} - {f.ArrivalTime:g})"
            }),
            "Id",
            "DisplayText",
            reservation.FlightId
        );
        ViewBag.FlightList = ViewData["FlightId"];
        ViewData["Email"] = email;

        return View(reservation);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ConfirmReservation(int id, string token)
    {
        var reservation = await _context.Reservations.FindAsync(id);

        if (reservation == null || reservation.ConfirmationToken != token)
        {
            return NotFound();
        }

        reservation.IsConfirmed = true;
        reservation.ConfirmationToken = null;
        reservation.ConfirmedAt = DateTime.UtcNow; // Set confirmation timestamp
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reservation confirmed successfully!";
        return RedirectToAction("Details", new { id });
    }

    /// <summary>
    /// Displays the reservation edit form.
    /// </summary>
    /// <param name="id">The reservation ID to edit.</param>
    /// <returns>The edit view or NotFound.</returns>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        ViewData["FlightId"] = new SelectList(_context.Flights, "Id", "AircraftNumber", reservation.FlightId);
        ViewData["ReservationUserId"] = new SelectList(_context.ReservationUsers, "Id", "UserName", reservation.ReservationUserId);
        return View(reservation);
    }

    /// <summary>
    /// Handles reservation edit form submission.
    /// </summary>
    /// <param name="id">The reservation ID being edited.</param>
    /// <param name="reservation">The updated reservation data.</param>
    /// <returns>Redirects to index view on success or returns edit view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FlightId,ReservationUserId,Nationality,TicketType")] Reservation reservation)
    {
        if (id != reservation.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(reservation);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(reservation.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["FlightId"] = new SelectList(_context.Flights, "Id", "AircraftNumber", reservation.FlightId);
        ViewData["ReservationUserId"] = new SelectList(_context.ReservationUsers, "Id", "UserName", reservation.ReservationUserId);
        return View(reservation);
    }

    /// <summary>
    /// Displays the reservation deletion confirmation form.
    /// </summary>
    /// <param name="id">The reservation ID to delete.</param>
    /// <returns>The deletion confirmation view or NotFound.</returns>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var reservation = await _context.Reservations
            .Include(r => r.Flight)
            .Include(r => r.ReservationUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (reservation == null) return NotFound();

        if (reservation.IsConfirmed)
        {
            TempData["ErrorMessage"] = "Confirmed reservations cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        return View(reservation);
    }

    /// <summary>
    /// Handles reservation deletion confirmation.
    /// </summary>
    /// <param name="id">The reservation ID to delete.</param>
    /// <param name="confirmDeleteUser">Whether to confirm deletion of associated user.</param>
    /// <returns>Redirects to index view.</returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, bool confirmDeleteUser = false)
    {
        var reservation = await _context.Reservations
            .Include(r => r.ReservationUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        if (reservation.IsConfirmed)
        {
            TempData["ErrorMessage"] = "Confirmed reservations cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        // Rest of your existing deletion logic...
        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        // Check if the ReservationUser has other reservations
        bool isLastReservation = !await _context.Reservations.AnyAsync(r => r.ReservationUserId == reservation.ReservationUserId);

        if (isLastReservation && reservation.ReservationUser != null && reservation.ReservationUser.AppUserId == null)
        {
            if (!confirmDeleteUser)
            {
                // Show a confirmation page instead of deleting immediately
                TempData["UserIdToDelete"] = reservation.ReservationUserId;
                return RedirectToAction(nameof(ConfirmDeleteUser), new { userId = reservation.ReservationUserId });
            }

            // If confirmed, delete the ReservationUser
            _context.ReservationUsers.Remove(reservation.ReservationUser);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the user deletion confirmation form.
    /// </summary>
    /// <param name="userId">The user ID to delete.</param>
    /// <returns>The confirmation view or NotFound.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmDeleteUser(int userId)
    {
        var user = await _context.ReservationUsers.FindAsync(userId);
        if (user == null) return NotFound();

        return View(user);
    }

    /// <summary>
    /// Handles user deletion confirmation.
    /// </summary>
    /// <param name="userId">The user ID to delete.</param>
    /// <returns>Redirects to index view.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmDeleteUserConfirmed(int userId)
    {
        var user = await _context.ReservationUsers.FindAsync(userId);
        if (user != null)
        {
            _context.ReservationUsers.Remove(user);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Checks if a reservation exists for the given EGN and flight ID.
    /// </summary>
    /// <param name="egn">The EGN to check.</param>
    /// <param name="flightId">The flight ID to check.</param>
    /// <returns>JSON response indicating if reservation exists.</returns>
    [AllowAnonymous]
    [HttpGet("CheckReservation")]
    public async Task<IActionResult> CheckReservation(string egn, int flightId)
    {
        var existingUser = await _context.ReservationUsers
            .FirstOrDefaultAsync(u => u.EGN == egn);

        if (existingUser == null)
        {
            return Json(new { exists = false });
        }

        var exists = await _context.Reservations
            .AnyAsync(r => r.FlightId == flightId && r.ReservationUserId == existingUser.Id);

        return Json(new { exists });
    }

    [AllowAnonymous]
    public IActionResult GroupCreate()
    {
        // Similar flight selection logic as in Create
        var flightsWithReservations = _context.Flights
            .Include(f => f.Reservations)
            .ToList();

        var availableFlights = flightsWithReservations
            .Where(f =>
            {
                var businessReserved = f.Reservations.Count(r => r.TicketType == TicketType.Business);
                var standardReserved = f.Reservations.Count(r => r.TicketType != TicketType.Business);
                var standardCapacity = f.PassengerCapacity - f.BusinessClassCapacity;

                return businessReserved < f.BusinessClassCapacity ||
                       standardReserved < standardCapacity;
            })
            .ToList();

        if (!availableFlights.Any())
        {
            ViewBag.NoFlightsAvailable = "Currently there are no flights with available seats.";
        }

        var flightList = availableFlights.Select(f => new SelectListItem
        {
            Value = f.Id.ToString(),
            Text = $"{f.AircraftNumber} - {f.FromLocation} → {f.ToLocation} ({f.DepartureTime.ToString("g")} - {f.ArrivalTime.ToString("g")})"
        }).ToList();

        ViewData["FlightList"] = flightList;

        // Initialize with one empty passenger
        var model = new GroupReservationViewModel
        {
            Passengers = new List<PassengerViewModel> { new PassengerViewModel() }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> GroupCreate(GroupReservationViewModel model)
    {
        // Validate flight availability
        var flight = await _context.Flights
            .Include(f => f.Reservations)
            .FirstOrDefaultAsync(f => f.Id == model.FlightId);

        if (flight == null)
        {
            ModelState.AddModelError("", "Selected flight does not exist.");
        }
        else
        {
            // Check seat availability for the group
            int requiredSeats = model.Passengers.Count;
            int availableSeats;

            if (model.TicketType == TicketType.Business)
            {
                var businessReserved = flight.Reservations.Count(r => r.TicketType == TicketType.Business);
                availableSeats = flight.BusinessClassCapacity - businessReserved;
            }
            else
            {
                var standardReserved = flight.Reservations.Count(r => r.TicketType != TicketType.Business);
                var standardCapacity = flight.PassengerCapacity - flight.BusinessClassCapacity;
                availableSeats = standardCapacity - standardReserved;
            }

            if (requiredSeats > availableSeats)
            {
                ModelState.AddModelError("", $"Not enough available seats. You requested {requiredSeats} but only {availableSeats} are available.");
            }
        }

        // Check for duplicate reservations
        foreach (var passenger in model.Passengers)
        {
            if (passenger.EGN?.Length != 10 || !passenger.EGN.All(char.IsDigit))
            {
                ModelState.AddModelError("", $"EGN must be exactly 10 digits for passenger {passenger.FirstName} {passenger.LastName}");
            }

            var existingUser = await _context.ReservationUsers
                .FirstOrDefaultAsync(u => u.EGN == passenger.EGN);

            if (existingUser != null)
            {
                bool hasExistingReservation = await _context.Reservations
                    .AnyAsync(r => r.FlightId == model.FlightId &&
                                  r.ReservationUserId == existingUser.Id);

                if (hasExistingReservation)
                {
                    ModelState.AddModelError("", $"Passenger with EGN {passenger.EGN} already has a reservation for this flight.");
                }
            }
        }

        if (ModelState.IsValid)
        {
            var reservations = new List<Reservation>();
            var confirmationToken = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow;

            // Process each passenger
            foreach (var passenger in model.Passengers)
            {
                // Check if user already exists
                var existingUser = await _context.ReservationUsers
                    .FirstOrDefaultAsync(u => u.EGN == passenger.EGN);

                if (existingUser != null)
                {
                    // Create reservation with existing user
                    var reservation = new Reservation
                    {
                        FlightId = model.FlightId,
                        ReservationUserId = existingUser.Id,
                        Nationality = model.Nationality,
                        TicketType = model.TicketType,
                        IsConfirmed = false,
                        ConfirmationToken = confirmationToken // Same token for whole group
                    };

                    reservations.Add(reservation);
                }
                else
                {
                    // Create new user and reservation
                    var newUser = new ReservationUser
                    {
                        UserName = passenger.UserName,
                        FirstName = passenger.FirstName,
                        MiddleName = passenger.MiddleName,
                        LastName = passenger.LastName,
                        EGN = passenger.EGN,
                        Address = passenger.Address,
                        PhoneNumber = passenger.PhoneNumber,
                        Email = model.Email
                    };

                    _context.ReservationUsers.Add(newUser);
                    await _context.SaveChangesAsync();

                    var reservation = new Reservation
                    {
                        FlightId = model.FlightId,
                        Nationality = model.Nationality,
                        ReservationUserId = newUser.Id,
                        TicketType = model.TicketType,
                        IsConfirmed = false,
                        ConfirmationToken = confirmationToken,
                        CreatedAt = createdAt
                    };

                    reservations.Add(reservation);
                }
            }

            // Add all reservations
            _context.Reservations.AddRange(reservations);
            await _context.SaveChangesAsync();

            var confirmationRouteValues = new
            {
                reservationIds = string.Join(",", reservations.Select(r => r.Id)),
                token = confirmationToken
            };
            var confirmationUrl = Url.Action(
                action: "ConfirmGroupReservation",
                controller: "Reservations",
                values: confirmationRouteValues,
                protocol: Request.Scheme);
            var baseDetailsUrl = Url.Action(
            action: "Details",
            controller: "Reservations",
            values: null,
            protocol: Request.Scheme);


            // Send confirmation email if provided
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                try
                {
                    var emailContent = GenerateReservationEmail.GroupReservationEmail(
                        reservations,
                        flight,
                        model.Passengers,
                        confirmationUrl,
                        baseDetailsUrl,
                        _templateService);

                    _emailService.SendEmail(
                        subject: $"Confirm Your Group Reservation #{reservations.First().Id}",
                        htmlContent: emailContent,
                        recipientEmail: model.Email,
                        recipientName: "Group Coordinator");
                }
                catch (Exception ex)
                {
                    // Log error but don't prevent reservation creation
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }
            }

            TempData["SuccessMessage"] = "Group reservation created! Please check your email to confirm.";
            return RedirectToAction("GroupCreate");
        }

        // If we got this far, something failed; redisplay form
        var flightsWithReservations = _context.Flights
            .Include(f => f.Reservations)
            .ToList();

        var availableFlights = flightsWithReservations
            .Where(f =>
            {
                var businessReserved = f.Reservations.Count(r => r.TicketType == TicketType.Business);
                var standardReserved = f.Reservations.Count(r => r.TicketType != TicketType.Business);
                var standardCapacity = f.PassengerCapacity - f.BusinessClassCapacity;

                return businessReserved < f.BusinessClassCapacity ||
                       standardReserved < standardCapacity;
            })
            .ToList();

        ViewData["FlightList"] = availableFlights.Select(f => new SelectListItem
        {
            Value = f.Id.ToString(),
            Text = $"{f.AircraftNumber} - {f.FromLocation} → {f.ToLocation} ({f.DepartureTime.ToString("g")} - {f.ArrivalTime.ToString("g")})"
        }).ToList();

        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ConfirmGroupReservation(string reservationIds, string token)
    {
        if (string.IsNullOrEmpty(reservationIds) || string.IsNullOrEmpty(token))
        {
            return NotFound();
        }
        var ids = reservationIds.Split(',').Select(int.Parse).ToList();
        var reservations = await _context.Reservations
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();

        if (!reservations.Any() || reservations.Any(r => r.ConfirmationToken != token))
        {
            return NotFound();
        }

        var confirmedAt = DateTime.UtcNow;
        foreach (var reservation in reservations)
        {
            reservation.IsConfirmed = true;
            reservation.ConfirmationToken = null;
            reservation.ConfirmedAt = confirmedAt;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Group reservation confirmed! {reservations.Count} tickets are now active.";
        return RedirectToAction("GroupConfirmation", new { reservationIds });
    }

    [AllowAnonymous]
    public async Task<IActionResult> GroupConfirmation(string reservationIds)
    {
        if (string.IsNullOrEmpty(reservationIds))
        {
            return NotFound();
        }

        var ids = reservationIds.Split(',').Select(int.Parse).ToList();
        var reservations = await _context.Reservations
            .Include(r => r.Flight)
            .Include(r => r.ReservationUser)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();

        if (!reservations.Any())
        {
            return NotFound();
        }

        return View(reservations);
    }

    private bool ReservationExists(int id)
    {
        return _context.Reservations.Any(e => e.Id == id);
    }
}