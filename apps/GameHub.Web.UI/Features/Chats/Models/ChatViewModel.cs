using MudBlazor;

namespace GameHub.Web.UI.Features.Chats.Models;

public class ChatViewModel
{
    public Guid Id { get; set; }
    public int ChannelId { get; set; }
    public string ChatAlias { get; set; } = default!;
    public Color AvatarColor { get; set; }
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int ParticipantsCount { get; set; }
    public string? LastMessagePreview { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }

    public class Comparer : IComparer<ChatViewModel>
    {
        public static readonly Comparer Instance = new();

        public int Compare(ChatViewModel? x, ChatViewModel? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return 1;
            if (y is null) return -1;

            var dateCompare = Nullable.Compare(y.LastMessageAt, x.LastMessageAt);
            if (dateCompare != 0) return dateCompare;

            return y.Id.CompareTo(x.Id);
        }
    }
}
