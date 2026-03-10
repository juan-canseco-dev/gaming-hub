using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace GameHub.Infrastructure.Data.Configurations;

public class UUIDv7Generator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry)
    {
        // Uses DateTimeOffset.UtcNow internally, so we don't need to pass it explicitly
        return Guid.CreateVersion7();
    }
}
