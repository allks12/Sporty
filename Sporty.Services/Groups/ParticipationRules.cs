using Sporty.Data.Entities;

namespace Sporty.Services.Groups;

public class ParticipationRules
{
    public static bool MeetsMin(int registeredCountIncludingOrganizer, int? minMembers)
    => registeredCountIncludingOrganizer >= minMembers;
    
    public static ParticipantStatus DecideStatus(int registeredCount, int? maxMembers) =>
        maxMembers.HasValue || registeredCount < maxMembers.Value
            ? ParticipantStatus.Registered
            : ParticipantStatus.Waitlisted;
    
    public static GroupMember? NextFromWaitlist(IEnumerable<GroupMember> members) =>
        members
            .Where(m => m.ParticipantStatus == ParticipantStatus.Waitlisted)
            .OrderBy(m => m.JoinedAt)
            .ThenBy(m => m.GroupMemberId)
            .FirstOrDefault();
}