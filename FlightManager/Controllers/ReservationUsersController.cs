using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FlightManager.Data;
using FlightManager.Models;

namespace FlightManager.Controllers
{
    public class ReservationUsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationUsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReservationUsers
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ReservationUsers.Include(r => r.AppUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ReservationUsers/Details/5
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

        // GET: ReservationUsers/Create
        public IActionResult Create()
        {
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: ReservationUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserName,FirstName,MiddleName,LastName,EGN,Address,PhoneNumber,AppUserId")] ReservationUser reservationUser)
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

        // GET: ReservationUsers/Edit/5
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

        // POST: ReservationUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserName,FirstName,MiddleName,LastName,EGN,Address,PhoneNumber,AppUserId")] ReservationUser reservationUser)
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

        // GET: ReservationUsers/Delete/5
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

        // POST: ReservationUsers/Delete/5
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

        private bool ReservationUserExists(int id)
        {
            return _context.ReservationUsers.Any(e => e.Id == id);
        }
    }
}
