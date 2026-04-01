namespace GameHub.Web.UI.Features.Channels.Models;

public sealed record SendMessageRequest(
     Guid ChatId,
     string Content
);