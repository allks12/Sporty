using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;
using Sporty.Services.Common;

namespace Sporty.Services.Groups;

public class GroupSearchService : IGroupSearchService
{
    private readonly SportyDbContext _context;
    private readonly TimeProvider _timeProvider;

    public GroupSearchService(
        SportyDbContext context,
        TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<OperationResult<GroupSearchResult>> SearchAsync(
    GroupSearchFilter filter,
    CancellationToken cancellationToken = default)
    {
        if (HasInvalidFilters(filter))
        {
            return OperationResult<GroupSearchResult>.Failure(
                ErrorCode.Validation,
                "Невалидни филтри за търсене.");
        }

        var query = ApplyFilters(CreateBaseQuery(), filter);

        const int pageSize = 9;

        var totalCount = await query.CountAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(
            (double)totalCount / pageSize);

        var page = Math.Min(
            filter.Page, Math.Max(1, totalPages));

        var items = await query
            .OrderBy(group => group.StartAt)
            .ThenBy(group => group.GroupId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(group => new GroupSearchItem
            {
                GroupId = group.GroupId,
                SportName = group.Sport.SportName,
                StartAt = group.StartAt,

                Location = group.Venue != null
                    ? group.Venue.Address
                    : group.CustomLocation ?? string.Empty,

                DistrictName = group.Venue != null
                    ? group.Venue.District.DistrictName
                    : group.District != null
                        ? group.District.DistrictName
                        : string.Empty,

                MinMembers = group.MinMembers,
                MaxMembers = group.MaxMembers,

                RegisteredMembers = group.Members.Count(member =>
                    member.ParticipantStatus ==
                        ParticipantStatus.Registered)
            })
            .ToListAsync(cancellationToken);

        var result = new GroupSearchResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return OperationResult<GroupSearchResult>.Success(result);
    }
    private DateTime GetCurrentBulgarianTime()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            "Europe/Sofia");

        var utcNow = _timeProvider.GetUtcNow();

        var bulgarianTime = TimeZoneInfo.ConvertTime(
            utcNow, timeZone);

        return bulgarianTime.DateTime;
    }

    private IQueryable<Group> CreateBaseQuery()
    {
        var now = GetCurrentBulgarianTime();

        return _context.Groups
            .AsNoTracking()
            .Where(group =>
                group.Status == GroupStatus.Open &&
                group.StartAt > now);
    }

    private IQueryable<Group> ApplyFilters(
    IQueryable<Group> query,
    GroupSearchFilter filter)
    {
        if (filter.SportId.HasValue)
        {
            query = query.Where(group =>
                group.SportId == filter.SportId.Value);
        }
        if (filter.DistrictId.HasValue)
        {
            var districtId = filter.DistrictId.Value;

            query = query.Where(group =>
                group.VenueId.HasValue
                    ? group.Venue != null &&
                      group.Venue.DistrictId == districtId
                    : group.DistrictId == districtId);
        }
        if (filter.LocationType == GroupLocationType.Venue)
        {
            query = query.Where(group => group.VenueId.HasValue);
        }
        else if (filter.LocationType == GroupLocationType.Custom)
        {
            query = query.Where(group => !group.VenueId.HasValue);
        }
        if (filter.OnlyAvailablePlaces)
        {
            query = query.Where(group =>
                group.Members.Count(member =>
                    member.ParticipantStatus == ParticipantStatus.Registered)
                < group.MaxMembers);
        }
        if (filter.Date.HasValue)
        {
            var selectedDate = filter.Date.Value;
            var dayStart = selectedDate.ToDateTime(TimeOnly.MinValue);

            query = query.Where(group => group.StartAt >= dayStart);

            if (selectedDate < DateOnly.MaxValue)
            {
                var nextDayStart = dayStart.AddDays(1);

                query = query.Where(group =>
                    group.StartAt < nextDayStart);
            }
        }
        return query;
    }
    private static bool HasInvalidFilters(GroupSearchFilter filter)
    {
        return filter.Page < 1
            || (filter.SportId.HasValue && filter.SportId.Value < 1)
            || (filter.DistrictId.HasValue && filter.DistrictId.Value < 1)
            || !Enum.IsDefined(filter.LocationType);
    }
    public async Task<IReadOnlyDictionary<int, string>> GetSportsAsync(
    CancellationToken cancellationToken = default)
    {
        return await _context.Sports
            .AsNoTracking()
            .OrderBy(sport => sport.SportName)
            .ToDictionaryAsync(
                sport => sport.SportId,
                sport => sport.SportName,
                cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetDistrictsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .AsNoTracking()
            .OrderBy(district => district.DistrictName)
            .ToDictionaryAsync(
                district => district.DistrictId,
                district => district.DistrictName,
                cancellationToken);
    }
}