namespace Sporty.Data.Entities;

public class District {
    public int DistrictId {get; set;}
    public string DistrictName {get; set;} = string.Empty;
    public ICollection<Venue> Venues {get; set;} = new List<Venue>();
}