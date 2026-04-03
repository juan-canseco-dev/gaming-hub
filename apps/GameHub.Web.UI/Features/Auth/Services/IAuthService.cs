using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Identity;
using GameHub.Web.UI.Features.Auth.Models;

namespace GameHub.Web.UI.Features.Auth.Services;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterUserRequest request);
    Task<Result<UserDetails>> LoginAsync(GetTokenRequest request);
}
