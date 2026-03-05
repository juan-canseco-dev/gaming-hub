namespace GameHub.Application.Abstractions.Clock;

public interface IDateTimeProvider
{
    DateTimeOffset CurrentTimeUtc { get; }
}
