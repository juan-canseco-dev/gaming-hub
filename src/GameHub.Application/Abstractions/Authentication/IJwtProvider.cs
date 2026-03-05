namespace GameHub.Application.Abstractions.Authentication;

public interface IJwtProvider
{
    Task<string> GenerateAsync(Guid userId, CancellationToken cancellationToken = default);
}
