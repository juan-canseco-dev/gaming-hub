using GameHub.Application.Abstractions.Clock;
using GameHub.Domain.Users;
using GameHub.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Data.Seed.Production;

internal class AdminUserSeeder : IProductionDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ILogger<AdminUserSeeder> _logger;

    public AdminUserSeeder(
        UserManager<ApplicationUser> userManager, 
        ApplicationDbContext context, 
        IDateTimeProvider timeProvider, 
        ILogger<AdminUserSeeder> logger
    )
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting admin user seeding...");

        var user = await _userManager.FindByEmailAsync(SystemUsers.AdminEmail);
        if (user is not null)
        {
            _logger.LogInformation(
                 "Admin user with email '{Email}' already exists. Skipping seeding.",
                  SystemUsers.AdminEmail
            );
            return;
        }
        var createdAt = _timeProvider.CurrentTimeUtc;

        var adminUser = new ApplicationUser
        {
            Id = SystemUsers.AdminUserId,
            Email = SystemUsers.AdminEmail,
            UserName = SystemUsers.AdminUsername,
            Fullname = SystemUsers.AdminName,
            CreatedAt = createdAt
        };

        var result = await _userManager.CreateAsync(adminUser, SystemUsers.AdminPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => $"{x.Code}: {x.Description}"));

            _logger.LogError(
                "Failed to create admin identity user '{Email}'. Errors: {Errors}",
                SystemUsers.AdminEmail,
                errors);

            throw new InvalidOperationException(
                $"Failed to seed admin identity user '{SystemUsers.AdminEmail}'. Errors: {errors}");
        }

        var adminProfile = new UserProfile(
            id: SystemUsers.AdminUserId, 
            email: SystemUsers.AdminEmail, 
            username: SystemUsers.AdminUsername, 
            fullname: SystemUsers.AdminName, 
            createdAt: createdAt
        );

        _context.UserProfiles.Add( adminProfile );

        _logger.LogInformation("Admin user and related profile were prepared successfully for seeding.");
    }

    public int Order => 1;
}
