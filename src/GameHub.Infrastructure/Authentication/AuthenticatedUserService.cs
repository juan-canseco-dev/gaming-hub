using GameHub.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace GameHub.Infrastructure.Authentication;

internal sealed class AuthenticatedUserService : IAuthenticatedUserService
{
    private readonly IHttpContextAccessor _contextAccessor;
    public AuthenticatedUserService(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }
    public Guid UserId
    {
        get
        {
            var uid = _contextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(uid) ? Guid.Empty : Guid.Parse(uid);
        }
    }
}
