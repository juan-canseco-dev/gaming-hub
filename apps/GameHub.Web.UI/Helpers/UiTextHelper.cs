namespace GameHub.Web.UI.Helpers;

public static class UiTextHelper
{
    public static string GetInitial(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        return char.ToUpper(value.Trim()[0]).ToString();
    }
}
