using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sporty.Data.Identity;

namespace Sporty.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.Name != null && u.Name.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Email)
            .ToListAsync();

        ViewBag.Search = search ?? string.Empty;
        ViewBag.CurrentUserId = _userManager.GetUserId(User);
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(string id, string? search = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToAction(nameof(Index), new { search });

        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["Error"] = "Не можете да блокирате собствения си акаунт.";
            return RedirectToAction(nameof(Index), new { search });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            user.LockoutEnabled = true;
            user.LockoutEnd = isLocked ? null : DateTimeOffset.UtcNow.AddYears(100);
            await _userManager.UpdateAsync(user);
        }

        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? search = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToAction(nameof(Index), new { search });

        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["Error"] = "Не можете да изтриете собствения си акаунт.";
            return RedirectToAction(nameof(Index), new { search });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }

        return RedirectToAction(nameof(Index), new { search });
    }
}