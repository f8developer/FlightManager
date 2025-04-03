using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Controllers
{
    /// <summary>
    /// Controller for handling administrative operations including user and role management.
    /// </summary>
    [Authorize(Roles = "Admin,Owner")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly OwnerSettings _ownerSettings;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            OwnerSettings ownerSettings)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
            _ownerSettings = ownerSettings;
        }

        /// <summary>
        /// Displays a list of users with optional filtering and pagination.
        /// </summary>
        public async Task<IActionResult> Index(string searchString, int? pageNumber, int? pageSize)
        {
            int currentPageSize = pageSize ?? 10;
            int currentPageNumber = pageNumber ?? 1;
            
            ViewBag.CurrentPageSize = currentPageSize;
            ViewBag.AvailablePageSizes = new List<int> { 5, 10, 20, 50 };
            ViewBag.CurrentFilter = searchString;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName.Contains(searchString) ||
                    u.Email.Contains(searchString) ||
                    u.PhoneNumber.Contains(searchString));
            }

            var paginatedUsers = await PaginatedList<AppUser>.CreateAsync(
                usersQuery.OrderBy(u => u.UserName).AsNoTracking(),
                currentPageNumber,
                currentPageSize);

            // Prepare role counts for display
            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in paginatedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();
            }
            ViewBag.UserRoles = userRoles;

            return View(paginatedUsers);
        }

        /// <summary>
        /// Displays a paginated list of all roles in the system with user counts for each role.
        /// </summary>
        /// <param name="pageNumber">The current page number for pagination.</param>
        /// <param name="pageSize">The number of items per page for pagination.</param>
        /// <returns>A view containing the paginated list of roles.</returns>
        public async Task<IActionResult> Roles(int? pageNumber, int? pageSize)
        {
            int currentPageSize = pageSize ?? 10;
            int currentPageNumber = pageNumber ?? 1;
            
            ViewBag.CurrentPageSize = currentPageSize;
            ViewBag.AvailablePageSizes = new List<int> { 5, 10, 20, 50 };

            var rolesQuery = _roleManager.Roles.AsQueryable();
            var paginatedRoles = await PaginatedList<IdentityRole>.CreateAsync(
                rolesQuery.OrderBy(r => r.Name).AsNoTracking(),
                currentPageNumber,
                currentPageSize);

            // Get user counts for each role
            var allUserRoles = await _context.UserRoles.ToListAsync();
            var roleGroups = allUserRoles.GroupBy(ur => ur.RoleId);

            var roleUserCounts = new Dictionary<string, int>();
            foreach (var role in paginatedRoles)
            {
                var count = roleGroups.FirstOrDefault(g => g.Key == role.Id)?.Count() ?? 0;
                roleUserCounts[role.Id] = count;
            }
            ViewBag.RoleUserCounts = roleUserCounts;

            return View(paginatedRoles);
        }

        /// <summary>
        /// Displays user details.
        /// </summary>
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        /// <summary>
        /// Displays user creation form.
        /// </summary>
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Create()
        {
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View();
        }

        /// <summary>
        /// Handles the creation of a new user with the specified details and roles.
        /// </summary>
        /// <param name="user">The user object containing basic information.</param>
        /// <param name="password">The password for the new user.</param>
        /// <param name="confirmPassword">The password confirmation which must match the password.</param>
        /// <param name="selectedRoles">List of role names to assign to the new user.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Create(
            [Bind("UserName,Email,PhoneNumber")] AppUser user,
            string password,
            string confirmPassword,
            List<string> selectedRoles)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match");
                ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(user);
            }

            user.UserName = user.Email;
            if (ModelState.IsValid)
            {
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (selectedRoles != null && selectedRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, selectedRoles);
                    }
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(user);
        }

        /// <summary>
        /// Displays user edit form.
        /// </summary>
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Email == _ownerSettings.OwnerEmail)
            {
                TempData["ErrorMessage"] = "The owner account cannot be modified.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);

            return View(user);
        }

        /// <summary>
        /// Handles updates to user information and role assignments.
        /// </summary>
        /// <param name="id">The ID of the user to edit.</param>
        /// <param name="user">The user object containing updated information.</param>
        /// <param name="selectedRoles">List of role names to assign to the user.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Edit(string id, [Bind("Id,UserName,Email,PhoneNumber")] AppUser user, List<string> selectedRoles)
        {
            if (id != user.Id) return NotFound();

            var existingUser = await _userManager.FindByIdAsync(id);
            if (existingUser == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingUser.Email = user.Email;
                existingUser.UserName = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;

                var result = await _userManager.UpdateAsync(existingUser);
                if (result.Succeeded)
                {
                    var currentRoles = await _userManager.GetRolesAsync(existingUser);
                    var rolesToAdd = selectedRoles?.Except(currentRoles) ?? Enumerable.Empty<string>();
                    var rolesToRemove = currentRoles.Except(selectedRoles ?? Enumerable.Empty<string>());

                    await _userManager.AddToRolesAsync(existingUser, rolesToAdd);
                    await _userManager.RemoveFromRolesAsync(existingUser, rolesToRemove);

                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.UserRoles = await _userManager.GetRolesAsync(existingUser);
            return View(user);
        }

        /// <summary>
        /// Displays user deletion confirmation.
        /// </summary>
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Email == _ownerSettings.OwnerEmail)
            {
                TempData["ErrorMessage"] = "The owner account cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        /// <summary>
        /// Permanently deletes a user from the system after confirmation.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>Redirects to Index with success or error message.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays role creation form.
        /// </summary>
        [Authorize(Roles = "Owner")]
        public IActionResult CreateRole()
        {
            return View();
        }

        /// <summary>
        /// Handles role creation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> CreateRole([Bind("Name")] IdentityRole role)
        {
            if (ModelState.IsValid)
            {
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Role '{role.Name}' created successfully.";
                    return RedirectToAction(nameof(Roles));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(role);
        }

        /// <summary>
        /// Displays role deletion confirmation.
        /// </summary>
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            if (id == null) return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            ViewBag.UserCount = usersInRole.Count;

            return View(role);
        }

        /// <summary>
        /// Permanently deletes a role from the system after confirmation, if no users are assigned to it.
        /// </summary>
        /// <param name="id">The ID of the role to delete.</param>
        /// <returns>Redirects to Roles view with success or error message.</returns>
        [HttpPost, ActionName("DeleteRole")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteRoleConfirmed(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            if (usersInRole.Count > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete role '{role.Name}' because it has {usersInRole.Count} assigned users.";
                return RedirectToAction(nameof(Roles));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{role.Name}' deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to delete role '{role.Name}'.";
            }

            return RedirectToAction(nameof(Roles));
        }
    }
}