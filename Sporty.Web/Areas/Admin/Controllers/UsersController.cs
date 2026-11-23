using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sporty.Data.Identity;
using Sporty.Services.Identity;

namespace Sporty.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly IUserAdminService _userAdminService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(
        IUserAdminService userAdminService,
        UserManager<ApplicationUser> userManager)
    {
        _userAdminService = userAdminService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search = null)
    {
        var users = await _userAdminService.ListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            users = users
                .Where(u =>
                    (u.Name != null && u.Name.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(term)))
                .ToList();
        }

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

        var result = await _userAdminService.DeleteAsync(id, currentUserId!);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Errors.FirstOrDefault() ?? "Възникна грешка при изтриването на потребителя.";
        }

        return RedirectToAction(nameof(Index), new { search });
    }
}