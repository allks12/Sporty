using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;

namespace Sporty.Services.Groups;

public record GroupCheckReport(int ConfirmedCount, int CancelledCount, int CompletedCount);

public interface IGroupCheckService
{
    Task<GroupCheckReport> RunAsync(CancellationToken cancellationToken = default);
}

public class GroupCheckService : IGroupCheckService
{
    private readonly SportyDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GroupCheckService> _logger;

    public GroupCheckService(
        SportyDbContext db,
        TimeProvider timeProvider,
        ILogger<GroupCheckService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<GroupCheckReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var deadlineUtc = DateTime.SpecifyKind(nowUtc.AddHours(24), DateTimeKind.Unspecified);
        
        var candidateIds = await _db.Groups
            .Where(g => (g.Status == GroupStatus.Open || g.Status == GroupStatus.Full)
                        && g.StartAt <= deadlineUtc)
            .Select(g => g.GroupId)
            .ToListAsync(cancellationToken);
        
        var confirmedCount = 0;
        var cancelledCount = 0;

        foreach (var groupId in candidateIds)
        {
            try
            {
                var outcome = await ProcessSingleGroupAsync(groupId, nowUtc, cancellationToken);
                if (outcome == GroupStatus.Confirmed)
                {
                    confirmedCount++;
                }
                else if (outcome == GroupStatus.Cancelled)
                {
                    cancelledCount++;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Грешка при автоматична проверка на група {GroupId}.", groupId);
            }
        }
        var completedCount = await MarkCompletedGroupsAsync(nowUtc, cancellationToken);
            
        return new GroupCheckReport(confirmedCount, cancelledCount, completedCount);
    }

    private async Task<GroupStatus?> ProcessSingleGroupAsync(
        int groupId, 
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        
        await _db.Database.ExecuteSqlRawAsync(
            """SELECT 1 FROM "Groups" WHERE "GroupId" = {0} FOR UPDATE;""",
            [groupId],
            cancellationToken);
        
        var group = await _db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.GroupId == groupId, cancellationToken);

        if (group is null || group.Status is not (GroupStatus.Open or GroupStatus.Full))
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
        
        var registeredCount = group.Members.Count(m => m.ParticipantStatus == ParticipantStatus.Registered);
        
        var minMembers = group.MinMembers ?? 2;
        var targetStatus = ParticipationRules.MeetsMin(registeredCount, minMembers)
            ? GroupStatus.Confirmed
            : GroupStatus.Cancelled;
        
        if (!GroupStateMachine.TryTransition(group, targetStatus))
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
        
        if (targetStatus == GroupStatus.Cancelled && group.TimeSlotId.HasValue)
        {
            await ReleaseTimeSlotIfPresentAsync(group.TimeSlotId.Value, cancellationToken);
        }
        
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return targetStatus;
    }
    
    private async Task ReleaseTimeSlotIfPresentAsync(int timeSlotId, CancellationToken cancellationToken)
    {
        var slot = await _db.TimeSlots.FirstOrDefaultAsync(t => t.TimeSlotId == timeSlotId, cancellationToken);
        if (slot is null)
        {
            return;
        }

        slot.IsReserved = false;
    }
    
    private async Task<int> MarkCompletedGroupsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var nowUnspecified = DateTime.SpecifyKind(nowUtc, DateTimeKind.Unspecified);
            
        var confirmedGroups = await _db.Groups
            .Where(g => g.Status == GroupStatus.Confirmed && g.StartAt <= nowUnspecified)
            .ToListAsync(cancellationToken);

        foreach (var group in confirmedGroups)
        {
            GroupStateMachine.TryTransition(group, GroupStatus.Completed);
        }

        if (confirmedGroups.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return confirmedGroups.Count;
    }
}