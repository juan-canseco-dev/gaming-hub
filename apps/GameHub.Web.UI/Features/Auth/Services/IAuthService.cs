using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Identity;
using GameHub.Web.UI.Features.Auth.Models;

namespace GameHub.Web.UI.Features.Auth.Services;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserDetails>> LoginAsync(GetTokenRequest request, CancellationToken cancellationToken = default);
}
