using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;
using Sporty.Data.Identity;
using Sporty.Services.Groups;

namespace Sporty.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class GroupsController : Controller
{
    private readonly SportyDbContext _context;
    private readonly IGroupService _groupService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GroupsController(
        SportyDbContext context,
        IGroupService groupService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _groupService = groupService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search = null, string? status = null)
    {
        var query = _context.Groups
            .Include(g => g.Sport)
            .Include(g => g.District)
            .Include(g => g.Venue)
            .Include(g => g.TimeSlot)
            .Include(g => g.Members)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(g =>
                (g.Sport != null && g.Sport.SportName.ToLower().Contains(term)) ||
                (g.Venue != null && g.Venue.Address.ToLower().Contains(term)) ||
                (g.CustomLocation != null && g.CustomLocation.ToLower().Contains(term)) ||
                (g.District != null && g.District.DistrictName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GroupStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(g => g.Status == parsedStatus);
        }

        var groups = await query
            .OrderByDescending(g => g.StartAt)
            .ToListAsync();

        var lastLog = await _context.GroupStatusCheckLogs
            .OrderByDescending(l => l.CheckedAt)
            .FirstOrDefaultAsync();

        ViewBag.Search = search ?? string.Empty;
        ViewBag.Status = status ?? string.Empty;
        ViewBag.LastCheck = lastLog != null
            ? lastLog.CheckedAt.ToLocalTime().ToString("dd.MM.yyyy • HH:mm")
            : "Няма извършена проверка";
        return View(groups);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? search = null, string? status = null)
    {
        var group = await _context.Groups
            .Include(g => g.Sport)
            .Include(g => g.TimeSlot)
            .FirstOrDefaultAsync(g => g.GroupId == id);

        if (group != null)
        {
            group.Status = GroupStatus.Cancelled;

            if (group.TimeSlot != null)
            {
                group.TimeSlot.IsReserved = false;
            }

            await _context.SaveChangesAsync();
            var label = group.Sport?.SportName ?? $"Група #{group.GroupId}";
            TempData["Success"] = $"Група „{label}“ беше отменена.";
        }

        return RedirectToAction(nameof(Index), new { search, status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunCheck(string? search = null, string? status = null)
    {
        var groups = await _context.Groups
            .Include(g => g.Members)
            .Include(g => g.TimeSlot)
            .Where(g => g.Status != GroupStatus.Cancelled)
            .ToListAsync();

        var updatedCount = 0;

        foreach (var g in groups)
        {
            var registeredCount = g.Members?.Count(m => m.ParticipantStatus == ParticipantStatus.Registered) ?? 0;
            var minRequired = g.MinMembers ?? 0;
            var previousStatus = g.Status;

            if (g.StartAt < DateTime.UtcNow && registeredCount < minRequired)
            {
                g.Status = GroupStatus.Cancelled;
                if (g.TimeSlot != null)
                {
                    g.TimeSlot.IsReserved = false;
                }
            }
            else if (g.MaxMembers > 0 && registeredCount >= g.MaxMembers)
            {
                g.Status = GroupStatus.Full;
            }
            else if (minRequired > 0 && registeredCount >= minRequired)
            {
                g.Status = GroupStatus.Confirmed;
            }
            else
            {
                g.Status = GroupStatus.Open;
            }

            if (g.Status != previousStatus)
            {
                updatedCount++;
            }
        }

        _context.GroupStatusCheckLogs.Add(new GroupStatusCheckLog
        {
            CheckedAt = DateTime.UtcNow,
            UpdatedCount = updatedCount
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Проверката приключи ({DateTime.Now:dd.MM.yyyy • HH:mm}). Обновени групи: {updatedCount}.";

        return RedirectToAction(nameof(Index), new { search, status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? search = null, string? status = null)
    {
        var result = await _groupService.DeleteAsync(id, _userManager.GetUserId(User)!, isAdmin: true);
        if (!result.Succeeded)
        {
            return NotFound();
        }

        TempData["Success"] = "Групата беше изтрита.";
        return RedirectToAction(nameof(Index), new { search, status });
    }
}