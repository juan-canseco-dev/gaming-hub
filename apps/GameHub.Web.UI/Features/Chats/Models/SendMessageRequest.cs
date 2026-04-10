namespace GameHub.Web.UI.Features.Chats.Models;

public sealed record SendMessageRequest(
     Guid ChatId,
     string Content
);
