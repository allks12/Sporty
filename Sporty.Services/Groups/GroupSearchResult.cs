namespace Sporty.Services.Groups;

public class GroupSearchResult
{
    public IReadOnlyList<GroupSearchItem> Items { get; set; }
        = new List<GroupSearchItem>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}