using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;
using Sporty.Services.Common;

namespace Sporty.Services.Groups;

public interface IGroupService {
    Task<OperationResult<int>> CreateAsync(string organizerId, CreateGroupInput input);
    Task<OperationResult<ParticipantStatus>> JoinAsync(int groupId, string userId, CancellationToken cancellationToken = default);
    Task<OperationResult<ParticipantStatus?>> LeaveAsync(int groupId, string userId, CancellationToken cancellationToken = default);
    Task<List<Group>> ListByOrganizerAsync(string userId);
    Task<List<Group>> ListAllAsync();
    Task<OperationResult<int>> DeleteAsync(int groupId, string userId, bool isAdmin);
    Task<OperationResult<int>> CreateForTimeSlotAsync(
        string organizerId,
        int timeSlotId,
        int sportId,
        int? districtId,
        int minMembers,
        int maxMembers,
        CancellationToken cancellationToken = default);
}

public class GroupService : IGroupService {
    private readonly SportyDbContext db;
    private readonly TimeProvider timeProvider;

    public GroupService(SportyDbContext db, TimeProvider timeProvider) {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    public async Task<OperationResult<int>> CreateAsync(string organizerId, CreateGroupInput input) {
        if (input.MaxMembers < input.MinMembers) {
            return OperationResult<int>.Failure(ErrorCode.Validation, "Максималният брой не може да е по-малък от минималния.");
        }

        var group = BuildGroup(organizerId, input);

        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return OperationResult<int>.Success(group.GroupId);
    }

    public async Task<OperationResult<ParticipantStatus>> JoinAsync(
        int groupId, 
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        
        await db.Database.ExecuteSqlRawAsync(
            """SELECT 1 FROM "Groups" WHERE "GroupId" = {0} FOR UPDATE;""",
            [groupId],
            cancellationToken);
        
        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.GroupId == groupId, cancellationToken);

        if (group is null)
        {
            return OperationResult<ParticipantStatus>.Failure(ErrorCode.NotFound, "Групата не съществува.");
        }

        var now = GetCurrentBulgarianTime();

        if (group.Status is GroupStatus.Cancelled or GroupStatus.Completed
            || group.StartAt <= now)
        {
            return OperationResult<ParticipantStatus>.Failure(ErrorCode.Conflict,
                "Включването в тази група не е възможно.");
        }

        if (group.Members.Any(m => m.UserId == userId))
        {
            return OperationResult<ParticipantStatus>.Failure(ErrorCode.Conflict, "Вече си записан в тази група.");
        }
        
        var registeredCount = group.Members.Count(m => m.ParticipantStatus == ParticipantStatus.Registered);
        var participantStatus = ParticipationRules.DecideStatus(registeredCount, group.MaxMembers);

        group.Members.Add(new GroupMember
        {
            UserId = userId,
            JoinedAt = DateTime.SpecifyKind(timeProvider.GetUtcNow().UtcDateTime, DateTimeKind.Utc),
            ParticipantStatus = participantStatus
        });

        if (participantStatus == ParticipantStatus.Registered
            && registeredCount + 1 >= group.MaxMembers)
        {
            GroupStateMachine.TryTransition(group, GroupStatus.Full);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        return OperationResult<ParticipantStatus>.Success(participantStatus);
    }
    
    public async Task<OperationResult<ParticipantStatus?>> LeaveAsync(
        int groupId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """SELECT 1 FROM "Groups" WHERE "GroupId" = {0} FOR UPDATE;""",
            [groupId],
            cancellationToken);

        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.GroupId == groupId, cancellationToken);

        if (group is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<ParticipantStatus?>.Failure(ErrorCode.NotFound, "Групата не съществува.");
        }

        if (group.OrganizerId == userId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<ParticipantStatus?>.Failure(ErrorCode.Forbidden, "Организаторът не може да напусне групата.");
        }

        var now = GetCurrentBulgarianTime();

        if (group.Status is GroupStatus.Cancelled or GroupStatus.Completed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<ParticipantStatus?>.Failure(ErrorCode.Conflict, "Напускането на тази група не е възможно.");
        }

        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<ParticipantStatus?>.Failure(ErrorCode.NotFound, "Не си записан в тази група.");
        }

        var wasRegistered = member.ParticipantStatus == ParticipantStatus.Registered;

        group.Members.Remove(member);
        db.GroupMembers.Remove(member);

        ParticipantStatus? promotedStatus = null;

        if (wasRegistered)
        {
            var nextWaitlisted = ParticipationRules.NextFromWaitlist(group.Members);

            if (nextWaitlisted is not null)
            {
                nextWaitlisted.ParticipantStatus = ParticipantStatus.Registered;
                promotedStatus = ParticipantStatus.Registered;
            }
            else if (group.Status == GroupStatus.Full)
            {
                GroupStateMachine.TryTransition(group, GroupStatus.Open);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OperationResult<ParticipantStatus?>.Success(promotedStatus);
    }
    
    private DateTime GetCurrentBulgarianTime()
    {
        var sofiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia");
        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), sofiaTimeZone).DateTime;
    }

    public Task<List<Group>> ListByOrganizerAsync(string userId) =>
        Query().Where(g => g.OrganizerId == userId).ToListAsync();

    public Task<List<Group>> ListAllAsync() =>
        Query().ToListAsync();

    //The organizer can delete their own group; an admin can delete any group
    public async Task<OperationResult<int>> DeleteAsync(int groupId, string userId, bool isAdmin) {
        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.GroupId == groupId);

        if (group is null) {
            return OperationResult<int>.Failure(ErrorCode.NotFound, "Групата не съществува.");
        }

        if (!isAdmin && group.OrganizerId != userId) {
            return OperationResult<int>.Failure(ErrorCode.Forbidden, "Можеш да изтриеш само своя група.");
        }

        db.GroupMembers.RemoveRange(group.Members);
        db.Groups.Remove(group);
        await db.SaveChangesAsync();
        return OperationResult<int>.Success(groupId);
    }

    public async Task<OperationResult<int>> CreateForTimeSlotAsync(
        string organizerId, 
        int timeSlotId, 
        int sportId, 
        int? districtId, 
        int minMembers,
        int maxMembers, 
        CancellationToken cancellationToken = default)
    {
        if (maxMembers < minMembers)
        {
            return OperationResult<int>.Failure(
                ErrorCode.Validation,
                "Максималният брой не може да е по-малък от минималния.");
        }
        
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        
        await db.Database.ExecuteSqlRawAsync(
            """SELECT 1 FROM "TimeSlots" WHERE "TimeSlotId" = {0} FOR UPDATE;""",
            [timeSlotId],
            cancellationToken);
        
        var slot = await db.TimeSlots
            .Include(t => t.Group)
            .FirstOrDefaultAsync(t => t.TimeSlotId == timeSlotId, cancellationToken);

        if (slot is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<int>.Failure(ErrorCode.Conflict,
                "Избраният час не съществува.");
        }

        if (slot.IsReserved || slot.Group is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<int>.Failure(ErrorCode.Conflict,
                "Избраният час вече е резервиран.");
        }
        
        var nowUtc = GetCurrentBulgarianTime();
        var maxAllowedStart = nowUtc.AddDays(7);
        
        var slotStart = DateTime.SpecifyKind(slot.StartAt, DateTimeKind.Unspecified);
        var nowUnspecified = DateTime.SpecifyKind(nowUtc, DateTimeKind.Unspecified);
        var maxAllowedUnspecified = DateTime.SpecifyKind(maxAllowedStart, DateTimeKind.Unspecified);

        if (slotStart <= nowUnspecified)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<int>.Failure(ErrorCode.Conflict,
                "Не може да се резервира минал или текущ час.");
        }

        if (slotStart > maxAllowedUnspecified)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<int>.Failure(ErrorCode.Validation,
                "Група с регистрирано игрище може да се създаде най-много до 7 дни напред.");
        }

        slot.IsReserved = true;

        var input = new CreateGroupInput
        {
            SportId = sportId,
            DistrictId = districtId,
            CustomLocation = string.Empty,
            Date = DateOnly.FromDateTime(slotStart),
            StartTime = TimeOnly.FromDateTime(slotStart),
            MinMembers = minMembers,
            MaxMembers = maxMembers
        };

        var created = await CreateAsync(organizerId, input);
        if (!created.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return created;
        }
        
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return created;
    }
    
    private Group BuildGroup(string organizerId, CreateGroupInput input)
    {
        var group = new Group
        {
            OrganizerId = organizerId,
            SportId = input.SportId!.Value,
            DistrictId = input.DistrictId,
            CustomLocation = input.CustomLocation!.Trim(),
            StartAt = input.Date!.Value.ToDateTime(input.StartTime!.Value),
            MinMembers = input.MinMembers,
            MaxMembers = input.MaxMembers!.Value,
            Status = GroupStatus.Open
        };

        // The organizer is the first member of the group
        group.Members.Add(new GroupMember
        {
            UserId = organizerId,
            JoinedAt = DateTime.SpecifyKind(timeProvider.GetUtcNow().UtcDateTime, DateTimeKind.Utc),
            ParticipantStatus = ParticipantStatus.Registered
        });

        return group;
    }

    private IQueryable<Group> Query() =>
        db.Groups
            .AsNoTracking()
            .Include(g => g.Sport)
            .Include(g => g.District)
            .Include(g => g.Organizer)
            .Include(g => g.Members)
            .OrderByDescending(g => g.StartAt);
}
