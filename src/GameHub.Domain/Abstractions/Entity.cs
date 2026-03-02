namespace GameHub.Domain.Abstractions;

public class Entity<TEntityId>
{
    protected Entity() { }
    protected Entity(TEntityId id)
    {
        Id = id;
    }
    public TEntityId? Id { get; init; }
}