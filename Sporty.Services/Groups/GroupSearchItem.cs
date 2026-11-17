namespace Sporty.Services.Groups;

public class GroupSearchItem
{
    public int GroupId { get; set; }

    public string SportName { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public string Location { get; set; } = string.Empty;

    public string DistrictName { get; set; } = string.Empty;

    public int? MinMembers { get; set; }

    public int MaxMembers { get; set; }

    public int RegisteredMembers { get; set; }

    public int AvailablePlaces =>
        Math.Max(0, MaxMembers - RegisteredMembers);
}