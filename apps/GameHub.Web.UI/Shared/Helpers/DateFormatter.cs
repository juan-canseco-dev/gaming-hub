namespace GameHub.Web.UI.Shared.Helpers;

public static class DateFormatter
{
    public static string ToChatFormat(DateTimeOffset value)
    {
        var now = DateTimeOffset.Now;

        if (value.Date == now.Date)
            return value.ToString("hh:mm tt");

        if (value.Date == now.AddDays(-1).Date)
            return $"Yesterday {value:hh:mm tt}";

        return value.ToString("dd/MM/yyyy hh:mm tt");
    }
}