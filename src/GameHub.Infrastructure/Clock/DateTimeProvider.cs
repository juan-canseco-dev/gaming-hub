using GameHub.Application.Abstractions.Clock;

namespace GameHub.Infrastructure.Clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset CurrentTimeUtc => DateTimeOffset.UtcNow;
}
