using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;
using Sporty.Services.Common;

namespace Sporty.Services.Groups;

public interface IGroupService {
    Task<OperationResult<int>> CreateAsync(string organizerId, CreateGroupInput input);
    Task<List<Group>> ListByOrganizerAsync(string userId);
    Task<List<Group>> ListAllAsync();
    Task<OperationResult<int>> DeleteAsync(int groupId, string userId, bool isAdmin);
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

        var group = new Group {
            OrganizerId = organizerId,
            SportId = input.SportId!.Value,
            DistrictId = input.DistrictId,
            CustomLocation = input.CustomLocation!.Trim(),
            StartAt = input.Date!.Value.ToDateTime(input.StartTime!.Value),
            MinMembers = input.MinMembers,
            MaxMembers = input.MaxMembers!.Value,
            Status = GroupStatus.Open
        };

        //The organizer is the first member of the group
        group.Members.Add(new GroupMember {
            UserId = organizerId,
            JoinedAt = timeProvider.GetUtcNow().UtcDateTime,
            ParticipantStatus = ParticipantStatus.Registered
        });

        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return OperationResult<int>.Success(group.GroupId);
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

    private IQueryable<Group> Query() =>
        db.Groups
            .AsNoTracking()
            .Include(g => g.Sport)
            .Include(g => g.District)
            .Include(g => g.Organizer)
            .Include(g => g.Members)
            .OrderByDescending(g => g.StartAt);
}
