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
public class VenuesController : Controller
{
    private readonly SportyDbContext _context;

    public VenuesController(SportyDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var venues = await _context.Venues
            .Include(v => v.Sport)
            .Include(v => v.District)
            .Include(v => v.Photos)
            .OrderBy(v => v.VenueId)
            .ToListAsync();

        return View(venues);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new Venue { IsOutdoors = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Venue venue)
    {
        ModelState.Remove(nameof(Venue.Sport));
        ModelState.Remove(nameof(Venue.District));
        ModelState.Remove(nameof(Venue.TimeSlots));
        ModelState.Remove(nameof(Venue.Photos));

        if (ModelState.IsValid)
        {
            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(venue.SportId, venue.DistrictId);
        return View(venue);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var venue = await _context.Venues.FindAsync(id);
        if (venue == null) return NotFound();

        await PopulateDropdownsAsync(venue.SportId, venue.DistrictId);
        return View(venue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Venue venue)
    {
        if (id != venue.VenueId) return NotFound();

        ModelState.Remove(nameof(Venue.Sport));
        ModelState.Remove(nameof(Venue.District));
        ModelState.Remove(nameof(Venue.TimeSlots));
        ModelState.Remove(nameof(Venue.Photos));

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(venue);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Venues.AnyAsync(e => e.VenueId == venue.VenueId))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(venue.SportId, venue.DistrictId);
        return View(venue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var venue = await _context.Venues.FindAsync(id);
        if (venue != null)
        {
            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPhoto(int venueId, string imageUrl)
    {
        var trimmedUrl = imageUrl?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedUrl) ||
            !Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            TempData["Error"] = "Невалиден URL адрес. Въведете коректен линк, започващ с http:// или https://.";
            return RedirectToAction(nameof(Index));
        }

        var venueExists = await _context.Venues.AnyAsync(v => v.VenueId == venueId);
        if (!venueExists)
            return NotFound();

        _context.VenuePhotos.Add(new VenuePhoto
        {
            VenueId = venueId,
            ImageUrl = parsedUri.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = "Снимката беше добавена успешно.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int photoId)
    {
        var photo = await _context.VenuePhotos.FindAsync(photoId);
        if (photo != null)
        {
            _context.VenuePhotos.Remove(photo);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Снимката беше премахната.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? selectedSportId = null, int? selectedDistrictId = null)
    {
        var sports = await _context.Sports.OrderBy(s => s.SportName).ToListAsync();
        var districts = await _context.Districts.OrderBy(d => d.DistrictName).ToListAsync();

        ViewBag.Sports = new SelectList(sports, nameof(Sport.SportId), nameof(Sport.SportName), selectedSportId);
        ViewBag.Districts = new SelectList(districts, nameof(District.DistrictId), nameof(District.DistrictName), selectedDistrictId);
    }
}