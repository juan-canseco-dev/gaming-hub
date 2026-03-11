namespace GameHub.WebAPI.IntegrationTests.Abstractions;

[CollectionDefinition(FixtureName)]
public class SharedTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string FixtureName = "SharedTestCollection";
}

