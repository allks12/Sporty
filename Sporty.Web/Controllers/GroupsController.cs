using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sporty.Services.Groups;
using Sporty.Web.Models;

namespace Sporty.Web.Controllers;

public class GroupsController : Controller
{
    private readonly IGroupSearchService _searchService;

    public GroupsController(IGroupSearchService searchService)
    {
        _searchService = searchService;
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

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
}