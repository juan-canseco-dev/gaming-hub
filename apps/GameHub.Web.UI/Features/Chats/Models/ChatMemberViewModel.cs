using GameHub.Contracts.Presence;
using MudBlazor;

namespace GameHub.Web.UI.Features.Chats.Models;

public sealed class ChatMemberViewModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Initial { get; set; } = "?";
    public string PresenceStatus { get; set; } = string.Empty;
    public bool IsYou { get; set; }
    public Color AvatarColor { get; set; }
    public DateTimeOffset? LastActive { get; set; }

    public sealed class Comparer : IComparer<ChatMemberViewModel>
    {
        public static readonly Comparer Instance = new();
        public int Compare(ChatMemberViewModel? x, ChatMemberViewModel? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var presenceCompare = Nullable.Compare(y.LastActive, x.LastActive);
            if (presenceCompare != 0)
                return presenceCompare;

            var usernameCompare = string.Compare(x.Username, y.Username, StringComparison.OrdinalIgnoreCase);
            if (usernameCompare != 0)
                return usernameCompare;

            return x.Id.CompareTo(y.Id);
        }
    }
}
