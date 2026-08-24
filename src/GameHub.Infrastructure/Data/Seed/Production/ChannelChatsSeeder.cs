using GameHub.Application.Abstractions.Clock;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Data.Seed.Production;

public class ChannelChatsSeeder : IProductionDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ILogger<ChannelChatsSeeder> _logger;

    public ChannelChatsSeeder(ApplicationDbContext context, IDateTimeProvider timeProvider, ILogger<ChannelChatsSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting channel chat seeding");
        if (await _context.Chats.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Skipping channel chat seeding because records already exist");
            return;
        }
        var newChats = Channel.GetValues()
            .Select(c => Chat.Create(c.Id, _timeProvider.CurrentTimeUtc).Value)
            .ToList();

        _context.AddRange(newChats);
        _logger.LogInformation("Prepared channel chats for seeding");
    }

    public int Order => 2;
}
