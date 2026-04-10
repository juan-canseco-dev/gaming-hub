namespace GameHub.Web.API.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string PolicyName { get; set; } = "MyCors";
    public string[] AllowedOrigins { get; set; } = [];
}