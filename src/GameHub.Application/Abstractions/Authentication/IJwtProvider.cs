namespace GameHub.Application.Abstractions.Authentication;

public interface IJwtProvider
{
    Task<string> GenerateAsync(string userId, CancellationToken cancellationToken = default);
}
