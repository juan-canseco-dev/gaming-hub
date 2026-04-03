using MudBlazor;

namespace GameHub.Web.UI.Features.Channels.Models;

public class ChatMessageViewModel
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public bool IsMine { get; set; }
    public string Initial { get; set; } = string.Empty; 
    public Color AvatarColor { get; set; } 
    public class Comparer : IComparer<ChatMessageViewModel>
    {
        public static readonly Comparer Instance = new();
        public int Compare(ChatMessageViewModel? x, ChatMessageViewModel? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var dateCompare = x.SentAt.CompareTo(y.SentAt);
            if (dateCompare != 0)
                return dateCompare;

            return x.Id.CompareTo(y.Id);
        }
    }
}
