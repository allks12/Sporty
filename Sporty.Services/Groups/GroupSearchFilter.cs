namespace Sporty.Services.Groups;

public enum GroupLocationType
{
    All,
    Venue,
    Custom
}

public class GroupSearchFilter
{
    public int? SportId { get; set; }

    public DateOnly? Date { get; set; }

    public int? DistrictId { get; set; }

    public GroupLocationType LocationType { get; set; }
        = GroupLocationType.All;

    public bool OnlyAvailablePlaces { get; set; }

    public int Page { get; set; } = 1;
}