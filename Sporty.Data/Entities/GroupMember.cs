using Sporty.Data.Identity;

namespace Sporty.Data.Entities;

public class GroupMember {
    public int GroupMemberId {get; set;}
    public int GroupId {get; set;}
    public Group Group {get; set;} = null!;
    public string UserId {get; set;} = string.Empty;
    public ApplicationUser User {get; set;} = null!;
    public DateTime JoinedAt {get; set;}
    public ParticipantStatus ParticipantStatus {get; set;}
}

public enum ParticipantStatus {
    Registered,
    Waitlisted
}