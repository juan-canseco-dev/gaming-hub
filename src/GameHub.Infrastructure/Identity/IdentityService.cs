using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Identity;
using GameHub.Application.Contracts.Identity;
using GameHub.Domain.Abstractions;
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
        _logger.LogInformation("Authentication attempt for Email: {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogWarning("Authentication failed. User not found for Email: {Email}", request.Email);
            return Result.Failure<GetTokenResponse>(IdentityErrors.InvalidCredentials);
        }

        _logger.LogDebug("User found for Email: {Email}. UserId: {UserId}", request.Email, user.Id);

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Authentication failed due to invalid password. UserId: {UserId}", user.Id);
            return Result.Failure<GetTokenResponse>(IdentityErrors.InvalidCredentials);
        }

        _logger.LogDebug("Password validated successfully. Generating JWT for UserId: {UserId}", user.Id);

        var jwt = await _tokenProvider.GenerateAsync(user.Id);

        _logger.LogInformation("JWT token generated successfully for UserId: {UserId}", user.Id);

        return new GetTokenResponse(jwt);
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting user registration for Username: {Username}, Email: {Email}", request.Username, request.Email);

        var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
        if (userWithSameEmail is not null)
        {
            _logger.LogWarning("Registration failed. Email already exists: {Email}", request.Email);
            return Result.Failure<Guid>(IdentityErrors.EmailAlreadyExists);
        }

        var userWithSameUsername = await _userManager.FindByNameAsync(request.Username);
        if (userWithSameUsername is not null)
        {
            _logger.LogWarning("Registration failed. Username already exists: {Username}", request.Username);
            return Result.Failure<Guid>(IdentityErrors.UsernameAlreadyExists);
        }

        var newUserId = Guid.CreateVersion7();
        var createdAt = _timeProvider.CurrentTimeUtc;

        _logger.LogDebug("Creating ApplicationUser instance with Id: {UserId}", newUserId);

        var newIdentityUser = new ApplicationUser
        {
            Id = newUserId,
            Fullname = request.Fullname,
            Email = request.Email,
            UserName = request.Username,
            CreatedAt = createdAt
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        _logger.LogDebug("Database transaction started for user registration. UserId: {UserId}", newUserId);

        try
        {
            _logger.LogDebug("Creating Identity user in ASP.NET Identity. UserId: {UserId}", newUserId);

            var result = await _userManager.CreateAsync(newIdentityUser, request.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Identity user creation failed for UserId: {UserId}. Errors: {@Errors}", newUserId, result.Errors);
                return Result.Failure<Guid>(MapIdentityErrors(result.Errors));
            }

            _logger.LogInformation("Identity user created successfully. UserId: {UserId}", newUserId);

            var newProfile = new UserProfile(
                id: newUserId,
                email: request.Email,
                username: request.Username,
                fullname: request.Fullname,
                createdAt: createdAt
            );

            _logger.LogDebug("Creating UserProfile entity for UserId: {UserId}", newUserId);

            _context.UserProfiles.Add(newProfile);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("UserProfile saved to database. UserId: {UserId}", newUserId);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("User registration completed successfully. UserId: {UserId}", newUserId);

            return Result.Success(newUserId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error occurred during user registration. Rolling back transaction. UserId: {UserId}", newUserId);
            
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
