using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Identity;
using GameHub.Web.UI.Authentication;

namespace GameHub.Web.UI.Services.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterUserRequest request);
    Task<Result<UserDetails>> LoginAsync(GetTokenRequest request);
}
