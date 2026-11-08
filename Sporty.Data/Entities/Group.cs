namespace Sporty.Data.Entities;

public class Group {
    public int GroupId {get; set;}
    public int SportId {get; set;}
    public Sport Sport {get; set;} = null!;
    public DateTime StartAt {get; set;}
    public int? MinMembers {get; set;}
    public int MaxMembers {get; set;}
    public GroupStatus Status {get; set;}

    public int? VenueId {get; set;}
    public Venue? Venue {get; set;}
    public int? TimeSlotId {get; set;}
    public TimeSlot? TimeSlot {get; set;}
    public string? CustomLocation {get; set;}
    public int? DistrictId {get; set;}
    public District? District {get; set;}

    public ICollection<GroupMember> Members {get; set;} = new List<GroupMember>();
}