using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Identity;
using GameHub.Abstractions.Primitives;
using GameHub.Domain.Users;
using GameHub.Infrastructure.Data;
using GameHub.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtProvider _tokenProvider;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        SignInManager<ApplicationUser> signInManager, 
        IJwtProvider tokenProvider, 
        IDateTimeProvider timeProvider, 
        ILogger<IdentityService> logger
    )
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<Result<GetTokenResponse>> GetTokenAsync(GetTokenRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting authentication");

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogInformation("Authentication rejected with {ErrorCode}", IdentityErrors.InvalidCredentials.Code);
            return Result.Failure<GetTokenResponse>(IdentityErrors.InvalidCredentials);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
        {
            _logger.LogInformation(
                "Authentication rejected for UserId {UserId} with {ErrorCode}",
                user.Id,
                IdentityErrors.InvalidCredentials.Code);
            return Result.Failure<GetTokenResponse>(IdentityErrors.InvalidCredentials);
        }

        var jwt = await _tokenProvider.GenerateAsync(user.Id);

        _logger.LogInformation("Authentication succeeded for UserId {UserId}", user.Id);

        return new GetTokenResponse(jwt);
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting user registration");

        var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
        if (userWithSameEmail is not null)
        {
            _logger.LogInformation(
                "User registration rejected with {ErrorCode}",
                IdentityErrors.EmailAlreadyExists.Code);
            return Result.Failure<Guid>(IdentityErrors.EmailAlreadyExists);
        }

        var userWithSameUsername = await _userManager.FindByNameAsync(request.Username);
        if (userWithSameUsername is not null)
        {
            _logger.LogInformation(
                "User registration rejected with {ErrorCode}",
                IdentityErrors.UsernameAlreadyExists.Code);
            return Result.Failure<Guid>(IdentityErrors.UsernameAlreadyExists);
        }

        var newUserId = Guid.CreateVersion7();
        var createdAt = _timeProvider.CurrentTimeUtc;

        _logger.LogDebug("Creating identity records for UserId {UserId}", newUserId);

        var newIdentityUser = new ApplicationUser
        {
            Id = newUserId,
            Fullname = request.Fullname,
            Email = request.Email,
            UserName = request.Username,
            CreatedAt = createdAt
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        _logger.LogDebug("Started user registration transaction for UserId {UserId}", newUserId);

        try
        {
            var result = await _userManager.CreateAsync(newIdentityUser, request.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Identity user creation failed for UserId {UserId} with error codes {IdentityErrorCodes}",
                    newUserId,
                    result.Errors.Select(error => error.Code).ToArray());
                return Result.Failure<Guid>(MapIdentityErrors(result.Errors));
            }

            var newProfile = new UserProfile(
                id: newUserId,
                email: request.Email,
                username: request.Username,
                fullname: request.Fullname,
                createdAt: createdAt
            );

            _logger.LogDebug("Adding user profile for UserId {UserId}", newUserId);

            _context.UserProfiles.Add(newProfile);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Persisted identity records for UserId {UserId}", newUserId);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("User registration succeeded for UserId {UserId}", newUserId);

            return Result.Success(newUserId);
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                "Rolling back user registration transaction for UserId {UserId} after {ExceptionType}",
                newUserId,
                e.GetType().Name);
            
            await transaction.RollbackAsync(cancellationToken);

            throw;
        }
    }

    private static Error MapIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var identityErrors = errors.ToList();


        var descriptions = identityErrors
            .Select(x => x.Description)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var message = string.Join(" | ", descriptions);

        return new Error(
            Code: "Identity.Validation",
            Description: message
        );
    }
}
