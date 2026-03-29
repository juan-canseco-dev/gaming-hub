namespace GameHub.Web.UI.Models;

public sealed record SendMessageRequest(
     Guid ChatId,
     string Content
);