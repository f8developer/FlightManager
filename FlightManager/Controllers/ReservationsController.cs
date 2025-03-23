using FlightManager.Data;
using FlightManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReservationsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Reservations
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var reservations = _context.Reservations
                .Include(r => r.Flight)
                .Include(r => r.ReservationUser);
            return View(await reservations.ToListAsync());
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Flight)
                .Include(r => r.ReservationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            return reservation == null ? NotFound() : View(reservation);
        }

        // GET: Reservations/Create
        public IActionResult Create()
        {
            ViewData["FlightId"] = new SelectList(_context.Flights, "Id", "AircraftNumber");
            ViewData["ReservationUserId"] = new SelectList(_context.ReservationUsers, "Id", "UserName");
            return View();
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FlightId,ReservationUserId,Nationality,TicketType")] Reservation reservation)
        {
            ModelState.Remove("Flight");
            ModelState.Remove("ReservationUser");

            if (ModelState.IsValid)
            {
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                if (_env.IsDevelopment())
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new
                        {
                            Field = x.Key,
                            Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                        })
                        .ToList();

                    ViewData["ModelErrors"] = errors;
                }
            }

            ViewData["FlightId"] = new SelectList(_context.Flights, "Id", "AircraftNumber", reservation.FlightId);
            ViewData["ReservationUserId"] = new SelectList(_context.ReservationUsers, "Id", "UserName", reservation.ReservationUserId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            ViewData["FlightId"] = new SelectList(_context.Flights, "Id", "AircraftNumber", reservation.FlightId);
            ViewData["ReservationUserId"] = new SelectList(_context.ReservationUsers, "Id", "UserName", reservation.ReservationUserId);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
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

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Flight)
                .Include(r => r.ReservationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            return reservation == null ? NotFound() : View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation != null)
            {
                // Remove reservation first
                _context.Reservations.Remove(reservation);

                // Check if ReservationUser is orphaned
                if (reservation.ReservationUser != null &&
                    !await _context.Reservations.AnyAsync(r => r.ReservationUserId == reservation.ReservationUserId) &&
                    reservation.ReservationUser.AppUserId == null)
                {
                    _context.ReservationUsers.Remove(reservation.ReservationUser);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}