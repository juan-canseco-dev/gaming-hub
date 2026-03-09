using FluentAssertions;

namespace GameHub.Application.UnitTests.Shared.Helpers;

public static class ReflectionTestHelper
{
    public static void SetProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        property.Should().NotBeNull($"Property '{propertyName}' was not found on type '{target.GetType().Name}'.");

        property!.SetValue(target, value);
    }
}
