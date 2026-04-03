using MudBlazor;

namespace GameHub.Web.UI.Shared.Helpers;

public static class AvatarColorHelper
{
    private static readonly Color[] Palette =
    {
        Color.Primary,
        Color.Secondary,
        Color.Tertiary,
        Color.Info,
        Color.Success,
        Color.Warning,
        Color.Error
    };

    public static Color GetColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Color.Default;

        var hash = value.Trim().ToUpperInvariant().GetHashCode();
        var index = Math.Abs(hash) % Palette.Length;

        return Palette[index];
    }
}