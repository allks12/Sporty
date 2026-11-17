using Sporty.Services.Common;

namespace Sporty.Services.Groups;

public interface IGroupSearchService
{
    Task<OperationResult<GroupSearchResult>> SearchAsync(
        GroupSearchFilter filter,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> GetSportsAsync(
    CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> GetDistrictsAsync(
        CancellationToken cancellationToken = default);
}