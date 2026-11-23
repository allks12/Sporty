using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;
using Sporty.Data.Identity;
using Sporty.Services.Common;
using Sporty.Services.Groups;
using Sporty.Web.Models;

namespace Sporty.Web.Controllers;

public class GroupsController : Controller
{
    private readonly IGroupSearchService _searchService;
    private readonly IGroupService _groupService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SportyDbContext _db;

    public GroupsController(
        IGroupSearchService searchService,
        IGroupService groupService,
        UserManager<ApplicationUser> userManager,
        SportyDbContext db)
    {
        _searchService = searchService;
        _groupService = groupService;
        _userManager = userManager;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [Bind(Prefix = "Filter")] GroupSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var model = new GroupSearchViewModel
        {
            Filter = filter
        };

        var sports = await _searchService.GetSportsAsync(
            cancellationToken);

        model.Sports = sports
            .Select(sport => new SelectListItem
            {
                Value = sport.Key.ToString(),
                Text = sport.Value
            })
            .ToList();

        var districts = await _searchService.GetDistrictsAsync(
            cancellationToken);

        model.Districts = districts
            .Select(district => new SelectListItem
            {
                Value = district.Key.ToString(),
                Text = district.Value
            })
            .ToList();

        if (!ModelState.IsValid)
        {
            model.Errors = new[] { "Невалидни филтри за търсене." };
            return View(model);
        }

        var result = await _searchService.SearchAsync(
            filter, cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            model.Result = result.Value;
        }
        else
        {
            model.Errors = result.Errors;
        }

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadLookupsAsync(cancellationToken);
        return View(new CreateGroupInput());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateGroupInput input,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = input.TimeSlotId.HasValue
                ? await _groupService.CreateForTimeSlotAsync(
                    _userManager.GetUserId(User)!,
                    input.TimeSlotId.Value,
                    input.SportId!.Value,
                    input.DistrictId,
                    input.MinMembers!.Value,
                    input.MaxMembers!.Value,
                    cancellationToken)
                : await _groupService.CreateAsync(
                    _userManager.GetUserId(User)!, input);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Mine));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        await LoadLookupsAsync(cancellationToken);
        return View(input);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id, CancellationToken cancellationToken)
    {
        var result = await _groupService.JoinAsync(
            id, 
            _userManager.GetUserId(User)!, 
            cancellationToken);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = result.Value == ParticipantStatus.Waitlisted
                ? "Включен си в чакащия списък на групата"
                : "Успешно се включи в групата";
            
            return RedirectToAction(nameof(Mine));
        }

        TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Неуспешно включване в групата";
        return RedirectToAction(nameof(Mine));
    }
    
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id, CancellationToken cancellationToken)
    {
        var result = await _groupService.LeaveAsync(
            id,
            _userManager.GetUserId(User)!,
            cancellationToken);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = result.Value == ParticipantStatus.Registered
                ? "Напусна групата. Първият от чакащия списък беше преместен като участник."
                : "Успешно напусна групата.";

            return RedirectToAction(nameof(Mine));
        }

        TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Неуспешно напускане на групата.";
        return RedirectToAction(nameof(Mine));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var groups = await _groupService.ListByOrganizerAsync(
            _userManager.GetUserId(User)!);

        return View(groups);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _groupService.DeleteAsync(
            id, _userManager.GetUserId(User)!, isAdmin: false);

        return result.ErrorCode switch
        {
            ErrorCode.NotFound => NotFound(),
            ErrorCode.Forbidden => Forbid(),
            _ => RedirectToAction(nameof(Mine))
        };
    }

    private async Task LoadLookupsAsync(
        CancellationToken cancellationToken)
    {
        var sports = await _searchService.GetSportsAsync(cancellationToken);
        var districts = await _searchService.GetDistrictsAsync(cancellationToken);

        var availableSlots = await _db.TimeSlots
            .Where(t => !t.IsReserved && t.Group == null)
            .OrderBy(t => t.StartAt)
            .Select(t => new
            {
                t.TimeSlotId,
                Label = $"Игрище #{t.VenueId} — {t.StartAt:dd.MM.yyyy HH:mm}–{t.EndAt:HH:mm}"
            })
            .ToListAsync(cancellationToken);

        ViewBag.Sports = new SelectList(sports, "Key", "Value");
        ViewBag.Districts = new SelectList(districts, "Key", "Value");
        ViewBag.TimeSlots = new SelectList(availableSlots, "TimeSlotId", "Label");
    }
}