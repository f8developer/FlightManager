using FlightManager.Data;
using FlightManager.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Controllers;

/// <summary>
/// Controller for handling administrative operations including user and role management.
/// </summary>
[Authorize(Roles = "Admin, Owner")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly OwnerSettings _ownerSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="env">The web hosting environment.</param>
    /// <param name="userManager">The user manager service.</param>
    /// <param name="roleManager">The role manager service.</param>
    /// <param name="ownerSettings">The owner configuration settings.</param>
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
    /// Displays a list of users with optional filtering by email and role.
    /// </summary>
    /// <param name="email">Email filter string.</param>
    /// <param name="role">Role filter string.</param>
    /// <returns>The view containing filtered users.</returns>
    public async Task<IActionResult> Index(string email, string role)
    {
        // Get all roles for the dropdown
        ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

        var usersQuery = _userManager.Users.AsQueryable();

        // Apply email filter if provided
        if (!string.IsNullOrEmpty(email))
        {
            usersQuery = usersQuery.Where(u => u.Email.Contains(email));
        }

        // Apply role filter if provided
        if (!string.IsNullOrEmpty(role))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            var userIds = usersInRole.Select(u => u.Id);
            usersQuery = usersQuery.Where(u => userIds.Contains(u.Id));
        }

        var users = await usersQuery.ToListAsync();

        // Prepare role counts for display
        var userRoles = new Dictionary<string, List<string>>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.ToList();
        }
        ViewBag.UserRoles = userRoles;

        return View(users);
    }

    /// <summary>
    /// Displays a list of all roles with their user counts.
    /// </summary>
    /// <returns>The view containing role information.</returns>
    public async Task<IActionResult> Roles()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var roleUserCounts = new Dictionary<string, int>();

        // Get all user-role mappings in one query
        var allUserRoles = await _context.UserRoles.ToListAsync();

        // Group by roleId and count users
        var roleGroups = allUserRoles.GroupBy(ur => ur.RoleId);

        foreach (var role in roles)
        {
            var count = roleGroups.FirstOrDefault(g => g.Key == role.Id)?.Count() ?? 0;
            roleUserCounts[role.Id] = count;
        }

        ViewBag.RoleUserCounts = roleUserCounts;
        return View(roles);
    }

    /// <summary>
    /// Displays details for a specific user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user details view or NotFound if user doesn't exist.</returns>
    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appUser = await _context.Users
            .FirstOrDefaultAsync(m => m.Id == id);
        if (appUser == null)
        {
            return NotFound();
        }

        return View(appUser);
    }

    /// <summary>
    /// Displays the user creation form.
    /// </summary>
    /// <returns>The user creation view.</returns>
    public async Task<IActionResult> Create()
    {
        ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View();
    }

    /// <summary>
    /// Handles user creation form submission.
    /// </summary>
    /// <param name="appUser">The user data.</param>
    /// <param name="password">The user password.</param>
    /// <param name="confirmPassword">The password confirmation.</param>
    /// <param name="selectedRoles">List of roles to assign to the user.</param>
    /// <returns>Redirects to user list on success or returns creation view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("UserName,Email,PhoneNumber")] AppUser appUser,
        string password,
        string confirmPassword,
        List<string> selectedRoles)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match");
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(appUser);
        }
        appUser.UserName = appUser.Email;
        if (ModelState.IsValid)
        {
            var result = await _userManager.CreateAsync(appUser, password);
            if (result.Succeeded)
            {
                // Add selected roles
                if (selectedRoles != null && selectedRoles.Any())
                {
                    var addRolesResult = await _userManager.AddToRolesAsync(appUser, selectedRoles);
                    if (!addRolesResult.Succeeded)
                    {
                        // Handle role assignment errors
                        foreach (var error in addRolesResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                        return View(appUser);
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            // Handle user creation errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
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
        ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(appUser);
    }

    /// <summary>
    /// Displays the user edit form.
    /// </summary>
    /// <param name="id">The user ID to edit.</param>
    /// <returns>The edit view or NotFound if user doesn't exist.</returns>
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Get all available roles
        var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        // Get user's current roles
        var userRoles = await _userManager.GetRolesAsync(user);

        ViewBag.AllRoles = allRoles;
        ViewBag.UserRoles = userRoles;

        return View(user);
    }

    /// <summary>
    /// Handles user edit form submission.
    /// </summary>
    /// <param name="id">The user ID being edited.</param>
    /// <param name="user">The updated user data.</param>
    /// <param name="selectedRoles">List of roles to assign to the user.</param>
    /// <returns>Redirects to user list on success or returns edit view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Id,UserName,Email,PhoneNumber")] AppUser user, List<string> selectedRoles)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        var existingUser = await _userManager.FindByIdAsync(id);
        if (existingUser == null)
        {
            return NotFound();
        }

        if (existingUser.Email == "owner@example.com")
        {
            TempData["ErrorMessage"] = "The owner account cannot be modified.";
            return RedirectToAction(nameof(Index));
        }

        if (ModelState.IsValid)
        {
            try
            {
                existingUser.Email = user.Email;
                existingUser.UserName = existingUser.Email;
                existingUser.PhoneNumber = user.PhoneNumber;

                // Update user details
                var result = await _userManager.UpdateAsync(existingUser);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                    ViewBag.UserRoles = await _userManager.GetRolesAsync(existingUser);
                    return View(user);
                }

                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(existingUser);
                var rolesToAdd = selectedRoles?.Except(currentRoles) ?? Enumerable.Empty<string>();
                var rolesToRemove = currentRoles.Except(selectedRoles ?? Enumerable.Empty<string>());

                await _userManager.AddToRolesAsync(existingUser, rolesToAdd);
                await _userManager.RemoveFromRolesAsync(existingUser, rolesToRemove);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AppUserExists(user.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
        return View(user);
    }

    /// <summary>
    /// Displays the user deletion confirmation form.
    /// </summary>
    /// <param name="id">The user ID to delete.</param>
    /// <returns>The deletion confirmation view or NotFound if user doesn't exist.</returns>
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRoles = roles;
        return View(user);
    }

    /// <summary>
    /// Handles user deletion confirmation.
    /// </summary>
    /// <param name="id">The user ID to delete.</param>
    /// <returns>Redirects to user list or returns error view.</returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.Email == _ownerSettings.OwnerEmail)
        {
            TempData["ErrorMessage"] = "The owner account cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(user);
        }

        TempData["SuccessMessage"] = "User deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> AppUserExists(string id)
    {
        return await _userManager.FindByIdAsync(id) != null;
    }

    /// <summary>
    /// Displays the role creation form.
    /// </summary>
    /// <returns>The role creation view.</returns>
    public IActionResult CreateRole()
    {
        return View();
    }

    /// <summary>
    /// Handles role creation form submission.
    /// </summary>
    /// <param name="role">The role data to create.</param>
    /// <returns>Redirects to role list on success or returns creation view with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
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
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(role);
    }

    /// <summary>
    /// Displays the role deletion confirmation form.
    /// </summary>
    /// <param name="id">The role ID to delete.</param>
    /// <returns>The deletion confirmation view or NotFound if role doesn't exist.</returns>
    public async Task<IActionResult> DeleteRole(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        // Check if any users are assigned to this role
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
        ViewBag.UserCount = usersInRole.Count;

        return View(role);
    }

    /// <summary>
    /// Handles role deletion confirmation.
    /// </summary>
    /// <param name="id">The role ID to delete.</param>
    /// <returns>Redirects to role list or returns error view.</returns>
    [HttpPost, ActionName("DeleteRole")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoleConfirmed(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

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