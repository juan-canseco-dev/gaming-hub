using GameHub.Application.Contracts.Identity;
using GameHub.Domain.Abstractions;

namespace GameHub.Application.Abstractions.Identity;

public interface IIdentityService 
{
    Task<Result<GetTokenResponse>> GetTokenAsync(GetTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result<Guid>> RegisterAsync(RegisterUserRequest request,CancellationToken cancellationToken = default);

}
