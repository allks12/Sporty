using Sporty.Data.Entities;

namespace Sporty.Services.Groups;

public class GroupStateMachine
{
    public static bool CanTransition(GroupStatus from, GroupStatus to) =>
        (from, to) switch
        {
            (GroupStatus.Open, GroupStatus.Full) => true,
            (GroupStatus.Open, GroupStatus.Confirmed) => true,
            (GroupStatus.Open, GroupStatus.Cancelled) => true,

            (GroupStatus.Full, GroupStatus.Open) => true,
            (GroupStatus.Full, GroupStatus.Confirmed) => true,
            (GroupStatus.Full, GroupStatus.Cancelled) => true,

            (GroupStatus.Confirmed, GroupStatus.Cancelled) => true,
            (GroupStatus.Confirmed, GroupStatus.Completed) => true,

            _ => false
        };
    
    public static bool TryTransition(Group group, GroupStatus to)
    {
        if (!CanTransition(group.Status, to))
        {
            return false;
        }

        group.Status = to;
        return true;
    }
}