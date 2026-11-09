namespace Sporty.Data.Entities;

public class Sport {
    public int SportId {get; set;}
    public string SportName {get; set;} = string.Empty;
    public ICollection<Venue> Venues {get; set;} = new List<Venue>();
    public ICollection<Group> Groups {get; set;} = new List<Group>();
}