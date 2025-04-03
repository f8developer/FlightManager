using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Controllers;

/// <summary>
/// Controller for managing reservation users.
/// </summary>
[Authorize(Roles = "Admin")]
public class ReservationUsersController : Controller
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationUsersController"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public ReservationUsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Displays a paginated list of reservation users with optional search filtering.
    /// Includes both standalone reservation users and those linked to application users.
    /// </summary>
    /// <param name="searchString">Optional search term to filter users by name, username, or email.</param>
    /// <param name="pageNumber">Current page number for pagination.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A view containing the filtered and paginated user list.</returns>
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Index(
        string searchString,
        int? pageNumber,
        int? pageSize)
    {
        int currentPageSize = pageSize ?? 10;
        int currentPageNumber = pageNumber ?? 1;

        ViewBag.CurrentPageSize = currentPageSize;
        ViewBag.AvailablePageSizes = new List<int> { 5, 10, 20, 50 };
        ViewBag.CurrentFilter = searchString;

        var usersQuery = _context.ReservationUsers
            .Include(u => u.AppUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            usersQuery = usersQuery.Where(u =>
                u.UserName.Contains(searchString) ||
                u.FirstName.Contains(searchString) ||
                u.LastName.Contains(searchString) ||
                u.Email.Contains(searchString) || // Added email to search
                (u.AppUser != null && u.AppUser.Email.Contains(searchString)));
        }

        var paginatedUsers = await PaginatedList<ReservationUser>.CreateAsync(
            usersQuery.AsNoTracking().OrderBy(u => u.LastName),
            currentPageNumber,
            currentPageSize);

        return View(paginatedUsers);
    }

    /// <summary>
    /// Displays details for a specific reservation user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user details view or NotFound.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reservationUser = await _context.ReservationUsers
            .Include(r => r.AppUser)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (reservationUser == null)
        {
            return NotFound();
        }

        return View(reservationUser);
    }

    /// <summary>
    /// Displays the reservation user creation form.
    /// </summary>
    /// <returns>The user creation view.</returns>
    public IActionResult Create()
    {
        ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id");
        return View();
    }

    /// <summary>
    /// Handles the creation of a new reservation user with the provided data.
    /// Validates the model and saves to database if valid.
    /// </summary>
    /// <param name="reservationUser">The reservation user data to create.</param>
    /// <returns>Redirects to index on success, or returns the creation view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,UserName,FirstName,MiddleName,LastName,EGN,Address,PhoneNumber,Email,AppUserId")] ReservationUser reservationUser)
    {
        if (ModelState.IsValid)
        {
            _context.Add(reservationUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", reservationUser.AppUserId);
        return View(reservationUser);
    }

    /// <summary>
    /// Displays the reservation user edit form.
    /// </summary>
    /// <param name="id">The user ID to edit.</param>
    /// <returns>The edit view or NotFound.</returns>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reservationUser = await _context.ReservationUsers.FindAsync(id);
        if (reservationUser == null)
        {
            return NotFound();
        }
        ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", reservationUser.AppUserId);
        return View(reservationUser);
    }

    /// <summary>
    /// Handles reservation user edit form submission.
    /// </summary>
    /// <param name="id">The user ID being edited.</param>
    /// <param name="reservationUser">The updated user data.</param>
    /// <returns>Redirects to index view on success or returns edit view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,UserName,FirstName,MiddleName,LastName,EGN,Address,PhoneNumber,Email,AppUserId")] ReservationUser reservationUser)
    {
        if (id != reservationUser.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(reservationUser);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationUserExists(reservationUser.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", reservationUser.AppUserId);
        return View(reservationUser);
    }

    /// <summary>
    /// Displays the reservation user deletion confirmation form.
    /// </summary>
    /// <param name="id">The user ID to delete.</param>
    /// <returns>The deletion confirmation view or NotFound.</returns>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reservationUser = await _context.ReservationUsers
            .Include(r => r.AppUser)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (reservationUser == null)
        {
            return NotFound();
        }

        return View(reservationUser);
    }

    /// <summary>
    /// Permanently deletes a reservation user after confirmation.
    /// Also removes any associated reservations if they exist.
    /// </summary>
    /// <param name="id">The ID of the user to delete.</param>
    /// <returns>Redirects to the index view after deletion.</returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var reservationUser = await _context.ReservationUsers.FindAsync(id);
        if (reservationUser != null)
        {
            _context.ReservationUsers.Remove(reservationUser);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Checks if a reservation user with the specified ID exists in the database.
    /// </summary>
    /// <param name="id">The ID of the reservation user to check.</param>
    /// <returns>True if the user exists, false otherwise.</returns>
    private bool ReservationUserExists(int id)
    {
        return _context.ReservationUsers.Any(e => e.Id == id);
    }
}