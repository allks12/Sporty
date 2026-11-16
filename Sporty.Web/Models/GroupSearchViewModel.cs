using Microsoft.AspNetCore.Mvc.Rendering;
using Sporty.Services.Groups;

namespace Sporty.Web.Models;

public class GroupSearchViewModel
{
    public GroupSearchFilter Filter { get; set; } = new();

    public List<SelectListItem> Sports { get; set; } = new();

    public List<SelectListItem> Districts { get; set; } = new();

    public GroupSearchResult Result { get; set; } = new();

    public IReadOnlyList<string> Errors { get; set; }
        = Array.Empty<string>();
}