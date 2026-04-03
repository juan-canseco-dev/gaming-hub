namespace GameHub.Web.UI.Infrastructure.Options;

public class ApiSettings
{
    public static readonly string SectionName = "ApiSettings";
    public required string BaseUrl { get; set; }
    public required string BaseHubUrl { get; set; }
}
