namespace GameHub.Application.Abstractions.Authentication;

public interface IAuthenticatedUserService
{
    public string? UserId { get; }
}
