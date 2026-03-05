namespace GameHub.Application.Abstractions.Authentication;

public interface IAuthenticatedUserService
{
    public Guid UserId { get; }
}
