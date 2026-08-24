using Bogus;
using GameHub.Application.Abstractions.Clock;
using GameHub.Contracts.Identity;
using GameHub.Domain.Users;
using GameHub.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Data.Seed.Development;

internal sealed class DemoUsersSeeder : IDevelopmentDataSeeder
{
    private const int DemoUsersCount = 50;
    private const string DefaultPassword = "Password.01";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ILogger<DemoUsersSeeder> _logger;

    public DemoUsersSeeder(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IDateTimeProvider timeProvider,
        ILogger<DemoUsersSeeder> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting demo user seeding");

        if (await _context.UserProfiles.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Demo users seeding skipped because user profiles already exist.");
            return;
        }

        var requests = CreateRegisterUserFaker().Generate(DemoUsersCount);
        var createdAt = _timeProvider.CurrentTimeUtc;
        var seededCount = 0;

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userId = Guid.CreateVersion7();

            var user = new ApplicationUser
            {
                Id = userId,
                Email = request.Email,
                UserName = request.Username,
                Fullname = request.Fullname,
                CreatedAt = createdAt
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(x => $"{x.Code}: {x.Description}"));

                _logger.LogError(
                    "Failed to create demo identity user '{Email}'. Errors: {Errors}",
                    request.Email,
                    errors);

                throw new InvalidOperationException(
                    $"Failed to seed identity user '{request.Email}'. Errors: {errors}");
            }

            var userProfile = new UserProfile(
                id: userId,
                email: request.Email,
                username: request.Username,
                fullname: request.Fullname,
                createdAt: createdAt
            );

            _context.UserProfiles.Add(userProfile);

            seededCount++;
            createdAt = createdAt.AddMinutes(5);
        }

        _logger.LogInformation(
            "Demo users seeding staged successfully. {Count} demo users prepared.",
            seededCount);
    }

    public int Order => 1;

    private static Faker<RegisterUserRequest> CreateRegisterUserFaker()
    {
        return new Faker<RegisterUserRequest>()
            .RuleFor(x => x.Fullname, f => f.Name.FullName())
            .RuleFor(x => x.Username, f => f.Internet.UserName())
            .RuleFor(x => x.Email, (_, x) => $"{x.Username}@mail.com")
            .RuleFor(x => x.Password, _ => DefaultPassword)
            .RuleFor(x => x.ConfirmPassword, _ => DefaultPassword);
    }
}
