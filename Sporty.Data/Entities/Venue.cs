namespace Sporty.Data.Entities;

public class Venue {
    public int VenueId {get; set;}
    public int SportId {get; set;}
    public Sport Sport {get; set;} = null!;
    public bool IsOutdoors {get; set;}
    public string Address {get; set;} = string.Empty;
    public decimal HourlyPrice {get; set;}
    public int DistrictId {get; set;}
    public District District {get; set;} = null!;

    public ICollection<TimeSlot> TimeSlots {get; set;} = new List<TimeSlot>();
}