using GameHub.Contracts.Presence;

namespace GameHub.Application.Abstractions.Presence;

public interface IPresenceService
{
    Task UpdateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPresenceDto> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPresenceDto>> GetByUsersAsync(List<Guid> userIds, CancellationToken cancellationToken = default);)
}
