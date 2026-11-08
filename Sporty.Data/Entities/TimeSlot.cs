namespace Sporty.Data.Entities;

public class TimeSlot {
    public int TimeSlotId {get; set;}
    public int VenueId {get; set;}
    public Venue Venue {get; set;} = null!;
    public DateTime StartAt {get; set;}
    public DateTime EndAt {get; set;}
    public bool IsReserved {get; set;}
    public Group? Group {get; set;}
}