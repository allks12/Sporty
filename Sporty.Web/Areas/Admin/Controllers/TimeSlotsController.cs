using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;
using Sporty.Data.Identity;

namespace Sporty.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class TimeSlotsController : Controller
{
    private readonly SportyDbContext _context;

    public TimeSlotsController(SportyDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? venueId = null)
    {
        var venues = await _context.Venues
            .Include(v => v.Sport)
            .Include(v => v.District)
            .OrderBy(v => v.VenueId)
            .ToListAsync();

        var selectedVenueId = venueId ?? venues.FirstOrDefault()?.VenueId;

        ViewBag.Venues = new SelectList(
            venues.Select(v => new
            {
                v.VenueId,
                Label = $"{v.Address} ({v.Sport?.SportName ?? "Спорт"})"
            }),
            "VenueId",
            "Label",
            selectedVenueId
        );
        ViewBag.SelectedVenueId = selectedVenueId;

        var query = _context.TimeSlots
            .Include(ts => ts.Venue)
            .AsQueryable();

        if (selectedVenueId.HasValue)
        {
            query = query.Where(ts => ts.VenueId == selectedVenueId.Value);
        }

        var slots = await query
            .OrderBy(ts => ts.StartAt)
            .ToListAsync();

        return View(slots);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? venueId = null)
    {
        await PopulateVenuesAsync(venueId);

        var defaultStart = DateTime.Today.AddHours(17);
        return View(new TimeSlot
        {
            VenueId = venueId ?? 0,
            StartAt = defaultStart,
            EndAt = defaultStart.AddHours(1),
            IsReserved = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimeSlot timeSlot)
    {
        ModelState.Remove(nameof(TimeSlot.Venue));
        ModelState.Remove(nameof(TimeSlot.Group));

        if (timeSlot.EndAt <= timeSlot.StartAt)
        {
            ModelState.AddModelError(nameof(TimeSlot.EndAt), "Крайният час трябва да е след началния.");
        }

        if (ModelState.IsValid)
        {
            _context.TimeSlots.Add(timeSlot);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { venueId = timeSlot.VenueId });
        }

        await PopulateVenuesAsync(timeSlot.VenueId);
        return View(timeSlot);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? venueId = null)
    {
        var slot = await _context.TimeSlots.FindAsync(id);
        var targetVenueId = venueId ?? slot?.VenueId;

        if (slot != null)
        {
            _context.TimeSlots.Remove(slot);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { venueId = targetVenueId });
    }

    private async Task PopulateVenuesAsync(int? selectedVenueId = null)
    {
        var venues = await _context.Venues
            .Include(v => v.Sport)
            .OrderBy(v => v.VenueId)
            .ToListAsync();

        var firstId = selectedVenueId ?? venues.FirstOrDefault()?.VenueId;
        ViewBag.Venues = new SelectList(
            venues.Select(v => new
            {
                v.VenueId,
                Label = $"{v.Address} ({v.Sport?.SportName ?? "Спорт"})"
            }),
            "VenueId",
            "Label",
            firstId
        );
    }
}