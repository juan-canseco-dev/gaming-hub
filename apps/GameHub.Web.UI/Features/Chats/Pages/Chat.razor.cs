using GameHub.Abstractions.Pagination;
using GameHub.Contracts.Chats;
using GameHub.Contracts.Notifications;
using GameHub.Contracts.Profile;
using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Features.Chats.Models;
using GameHub.Web.UI.Features.Chats.Services.Interfaces;
using GameHub.Web.UI.Features.Channels.Services.Interfaces;
using GameHub.Web.UI.Shared.Constants;
using GameHub.Web.UI.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using static GameHub.Web.UI.Shared.Helpers.AvatarColorHelper;


namespace GameHub.Web.UI.Features.Chats.Pages;

public partial class Chat : ComponentBase, IAsyncDisposable
{

    private readonly ILogger<Chat> _logger;
    public Chat(ILogger<Chat> logger)
    {
        _logger = logger;
    }

    [CascadingParameter(Name = "HubConnection")]
    public required HubConnection HubConnection { get; set; }
    [Parameter]
    public Guid ChatId { get; set; }

    [Inject]
    public required JwtAuthenticationStateProvider AuthProvider { get; set; }
    [Inject]
    public required IChatService ChatService { get; set; }
    [Inject]
    public required IChannelsService ChannelsService { get; set; }
    [Inject]
    public required IJSRuntime JS { get; set; }
    private const int MessagesPageSize = 30;
    private const int MembersPageSize = 30;
    private static readonly TimeSpan PresenceRefreshInterval = TimeSpan.FromSeconds(30);

    private Guid UserId = Guid.Empty;

    public required MudTextField<string> _messageTextField;

    private string ChatTitle = string.Empty;
    private string ChatDescription = string.Empty;
    private int ParticipantsCount = 0;
    private int OnlineUsersCount = 0;

    private string _messageText = string.Empty;
    private string? _lastFailedMessageText;

    private bool _isHeaderLoading = true;
    private bool _isMessagesLoading = true;
    private bool _isMembersLoading = true;
    private bool _isLoadingMoreMessages;
    private bool _isLoadingMoreMembers;
    private bool _isSendingMessage;
    private bool _isMessagesNearBottom = true;
    private bool _scrollToBottomAfterRender;
    private bool _smoothScrollAfterRender;
    private int _newMessagesCount;

    private string? _headerError;
    private string? _messagesError;
    private string? _membersError;
    private string? _messagesPagingError;
    private string? _membersPagingError;
    private string? _sendMessageError;

    private string? _messagesNextCursor;
    private string? _membersNextCursor;

    private readonly List<ChatMessageViewModel> Messages = new();
    private readonly List<ChatMemberViewModel> Members = new();
    private readonly List<IDisposable> _hubSubscriptions = new();
    private readonly CancellationTokenSource _presenceRefreshCancellation = new();
    private Task? _presenceRefreshTask;

    private ElementReference _messagesContainerRef;
    private ElementReference _messagesTopSentinelRef;
    private ElementReference _membersContainerRef;
    private ElementReference _membersBottomSentinelRef;

    private DotNetObjectReference<Chat>? _dotNetRef;
    private bool _observersInitialized;
    private bool _initialMessagesScrolled;

    protected override async Task OnInitializedAsync()
    {
        if (ChatId == Guid.Empty)
        {
            _headerError = "Invalid chat id.";
            _messagesError = "Invalid chat id.";
            _membersError = "Invalid chat id.";
            _isHeaderLoading = false;
            _isMessagesLoading = false;
            _isMembersLoading = false;
            return;
        }

        var details = await AuthProvider.GetAuthenticatedUserDetailsAsync();
        UserId = details is null ? Guid.Empty : details.Id;

        RegisterHubSubscriptions();

        await LoadAllAsync();
        _presenceRefreshTask = RefreshPresenceStatusesAsync(_presenceRefreshCancellation.Token);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_observersInitialized &&
            !_isMessagesLoading &&
            !_isMembersLoading)
        {
            _dotNetRef = DotNetObjectReference.Create(this);

            await JS.InvokeVoidAsync(
                "chatChannelPage.init",
                _dotNetRef,
                _messagesContainerRef,
                _messagesTopSentinelRef,
                _membersContainerRef,
                _membersBottomSentinelRef);

            _observersInitialized = true;
        }

        if (!_initialMessagesScrolled &&
            Messages.Count > 0 &&
            !_isMessagesLoading)
        {
            _initialMessagesScrolled = true;
            await Task.Delay(100);
            await ScrollMessagesToBottomAsync(smooth: false);
            _isMessagesNearBottom = true;
        }

        if (_scrollToBottomAfterRender)
        {
            var smooth = _smoothScrollAfterRender;
            _scrollToBottomAfterRender = false;
            _smoothScrollAfterRender = false;
            await ScrollMessagesToBottomAsync(smooth);
        }
    }

    private void RegisterHubSubscriptions()
    {
        _hubSubscriptions.Add(HubConnection.On<UserJoinedNotification>(
            GameHubConstants.SignalR.UserJoinedChat,
            notification => InvokeAsync(() => HandleUserJoinedAsync(notification))));

        _hubSubscriptions.Add(HubConnection.On<MessageNotification>(
            GameHubConstants.SignalR.MessageSent,
            notification => InvokeAsync(() => HandleMessageSentAsync(notification))));

        _hubSubscriptions.Add(HubConnection.On<UserPresenceUpdatedNotification>(
            GameHubConstants.SignalR.UserPresenceUpdated,
            notification => InvokeAsync(() => HandlePresenceUpdated(notification))));
    }

    private async Task HandleUserJoinedAsync(UserJoinedNotification notification)
    {
        if (notification.ChatId != ChatId)
            return;

        ParticipantsCount = notification.NumberOfParticipants;
        await HandleRealtimeMessageAsync(notification.Message);
    }

    private async Task HandleMessageSentAsync(MessageNotification notification)
    {
        if (notification.ChatId != ChatId)
            return;

        await HandleRealtimeMessageAsync(notification.Message);
    }

    private void HandlePresenceUpdated(UserPresenceUpdatedNotification notification)
    {
        OnlineUsersCount = notification.OnlineUsersCount;

        var member = Members.FirstOrDefault(x => x.Id == notification.Presence.UserId);
        if (member is not null)
        {
            member.LastActive = notification.Presence.LastActive;
            member.PresenceStatus = GetPresenceStatus(member.LastActive);
            Members.Sort(ChatMemberViewModel.Comparer.Instance);
        }

        StateHasChanged();
    }

    private async Task HandleRealtimeMessageAsync(MessageDto dto)
    {
        if (!TryAddMessage(dto, out var message))
            return;

        var shouldScrollToBottom = _isMessagesNearBottom || message.IsMine;
        if (shouldScrollToBottom)
        {
            _newMessagesCount = 0;
            QueueScrollToBottom(smooth: false);
        }
        else
        {
            _newMessagesCount++;
        }

        await InvokeAsync(StateHasChanged);

        if (shouldScrollToBottom)
        {
            await TryMarkChatAsReadAsync();
        }
    }

    private async Task LoadAllAsync()
    {
        await LoadHeaderAsync();
        await LoadInitialMessagesAsync();
        await LoadInitialMembersAsync();
        await HubConnection.InvokeAsync(GameHubConstants.SignalR.JoinChat, ChatId);
        await HubConnection.InvokeAsync(GameHubConstants.SignalR.UpdatePresence);
    }

    private async Task LoadHeaderAsync()
    {
        _isHeaderLoading = true;
        _headerError = null;
        await InvokeAsync(StateHasChanged);

        var result = await ChatService.GetByIdAsync(ChatId);

        if (result.IsFailure)
        {
            _headerError = result.Error.Description;
            _isHeaderLoading = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        var chat = result.Value;
        ChatTitle = chat.Slug;
        ChatDescription = chat.Description;
        ParticipantsCount = chat.ParticipantsCount;

        _isHeaderLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadInitialMessagesAsync()
    {
        _isMessagesLoading = true;
        _messagesError = null;
        _messagesPagingError = null;
        await InvokeAsync(StateHasChanged);

        Messages.Clear();
        _messagesNextCursor = null;

        var result = await ChatService.GetMessagesAsync(ChatId, MessagesPageSize);

        if (result.IsFailure)
        {
            _messagesError = result.Error.Description;
            _isMessagesLoading = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        MergeMessages(result.Value.Items);
        _messagesNextCursor = BuildMessagesNextCursor(result.Value);

        await TryMarkChatAsReadAsync();

        _isMessagesLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadInitialMembersAsync()
    {
        _isMembersLoading = true;
        _membersError = null;
        _membersPagingError = null;
        await InvokeAsync(StateHasChanged);

        Members.Clear();
        _membersNextCursor = null;

        var result = await ChannelsService.GetParticipantsAsync(ChatId, MembersPageSize);

        if (result.IsFailure)
        {
            _membersError = result.Error.Description;
            _isMembersLoading = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        MergeMembers(result.Value.Items);
        _membersNextCursor = BuildMembersNextCursor(result.Value);
        ParticipantsCount = Math.Max(ParticipantsCount, Members.Count);

        _isMembersLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RetryHeaderAsync() => await LoadHeaderAsync();
    private async Task RetryMessagesAsync() => await LoadInitialMessagesAsync();
    private async Task RetryMembersAsync() => await LoadInitialMembersAsync();
    private async Task RetryMessagesPagingAsync() => await LoadMoreMessagesAsync();
    private async Task RetryMembersPagingAsync() => await LoadMoreMembersAsync();
    private async Task RetrySendMessageAsync() => await SendMessageCoreAsync(_lastFailedMessageText);

    [JSInvokable]
    public async Task OnMessagesTopReached()
    {
        await LoadMoreMessagesAsync();
    }

    [JSInvokable]
    public async Task OnMembersBottomReached()
    {
        await LoadMoreMembersAsync();
    }

    [JSInvokable]
    public async Task OnMessagesBottomStateChanged(bool isNearBottom)
    {
        _isMessagesNearBottom = isNearBottom;

        if (!isNearBottom || _newMessagesCount == 0)
            return;

        _newMessagesCount = 0;
        await InvokeAsync(StateHasChanged);
        await TryMarkChatAsReadAsync();
    }

    private async Task LoadMoreMessagesAsync()
    {
        if (_isMessagesLoading || _isLoadingMoreMessages)
            return;

        if (string.IsNullOrWhiteSpace(_messagesNextCursor))
            return;

        _isLoadingMoreMessages = true;
        _messagesPagingError = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var previousScrollHeight = await JS.InvokeAsync<double>(
                "chatChannelPage.getScrollHeight",
                _messagesContainerRef);

            var result = await ChatService.GetMessagesAsync(ChatId, MessagesPageSize, _messagesNextCursor);

            if (result.IsFailure)
            {
                _messagesPagingError = result.Error.Description;
                _isLoadingMoreMessages = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            var existingOldestId = Messages.Count == 0
                ? Guid.Empty
                : Messages[0].Id;

            MergeMessages(result.Value.Items);
            _messagesNextCursor = BuildMessagesNextCursor(result.Value);

            await InvokeAsync(StateHasChanged);

            if (Messages.Count > 0 && Messages[0].Id != existingOldestId)
            {
                await JS.InvokeVoidAsync(
                    "chatChannelPage.restoreScrollAfterPrepend",
                    _messagesContainerRef,
                    previousScrollHeight);
            }
        }
        catch
        {
            _messagesPagingError = "Failed to load older messages.";
        }

        _isLoadingMoreMessages = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadMoreMembersAsync()
    {
        if (_isMembersLoading || _isLoadingMoreMembers)
            return;

        if (string.IsNullOrWhiteSpace(_membersNextCursor))
            return;

        _isLoadingMoreMembers = true;
        _membersPagingError = null;
        await InvokeAsync(StateHasChanged);

        var result = await ChannelsService.GetParticipantsAsync(ChatId, MembersPageSize, _membersNextCursor);

        if (result.IsFailure)
        {
            _membersPagingError = result.Error.Description;
            _isLoadingMoreMembers = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        MergeMembers(result.Value.Items);
        _membersNextCursor = BuildMembersNextCursor(result.Value);
        ParticipantsCount = Math.Max(ParticipantsCount, Members.Count);

        _isLoadingMoreMembers = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SendMessageAsync()
    {
        await SendMessageCoreAsync(_messageText);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SendMessageAsync();
        }
    }

    private async Task SendMessageCoreAsync(string? content)
    {
        if (_isSendingMessage)
            return;

        var normalizedContent = content?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedContent))
            return;

        _isSendingMessage = true;
        _sendMessageError = null;
        _lastFailedMessageText = normalizedContent;
        await InvokeAsync(StateHasChanged);

        var request = new SendMessageRequest(ChatId, normalizedContent);
        var result = await ChatService.SendMessageAsync(request);

        if (result.IsFailure)
        {
            _sendMessageError = result.Error.Description;
            _isSendingMessage = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        TryAddMessage(result.Value, out _);

        _messageText = string.Empty;
        _lastFailedMessageText = null;
        _newMessagesCount = 0;
        _isMessagesNearBottom = true;
        QueueScrollToBottom(smooth: false);
        _isSendingMessage = false;

        await InvokeAsync(StateHasChanged);
        await _messageTextField.FocusAsync();
        await TryMarkChatAsReadAsync();
    }

    private async Task ScrollToNewMessagesAsync()
    {
        _newMessagesCount = 0;
        _isMessagesNearBottom = true;
        QueueScrollToBottom(smooth: true);
        await TryMarkChatAsReadAsync();
    }

    private void QueueScrollToBottom(bool smooth)
    {
        _scrollToBottomAfterRender = true;
        _smoothScrollAfterRender = smooth;
    }

    private async Task ScrollMessagesToBottomAsync(bool smooth)
    {
        await JS.InvokeVoidAsync("chatChannelPage.scrollToBottom", _messagesContainerRef, smooth);
    }

    private string NewMessagesLabel => _newMessagesCount == 1
        ? "1 new message"
        : $"{_newMessagesCount} new messages";

    private async Task TryMarkChatAsReadAsync()
    {
        try
        {
            await ChatService.MarkChatAsReadAsync(ChatId);
        }
        catch
        {
        }
    }

    private void MergeMessages(IEnumerable<MessageDto> items)
    {
        var incoming = items
            .Select(MapMessage)
            .ToList();

        foreach (var item in incoming)
        {
            if (Messages.Any(x => x.Id == item.Id))
                continue;

            Messages.Add(item);
        }

        Messages.Sort(ChatMessageViewModel.Comparer.Instance);
    }

    private bool TryAddMessage(MessageDto dto, out ChatMessageViewModel message)
    {
        var mappedMessage = MapMessage(dto);
        message = mappedMessage;

        if (Messages.Any(x => x.Id == mappedMessage.Id))
            return false;

        Messages.Add(mappedMessage);
        Messages.Sort(ChatMessageViewModel.Comparer.Instance);
        return true;
    }

    private void MergeMembers(IEnumerable<UserDto> items)
    {
        var incoming = items
            .Select(MapMember)
            .ToList();

        foreach (var item in incoming)
        {
            if (Members.Any(x => x.Id == item.Id))
                continue;

            Members.Add(item);
        }

        Members.Sort(ChatMemberViewModel.Comparer.Instance);
    }

    private ChatMessageViewModel MapMessage(MessageDto dto)
    {
        var authorName = dto.IsSystem
            ? "System"
            : dto.User?.Fullname?.Trim();

        if (string.IsNullOrWhiteSpace(authorName))
            authorName = dto.User?.Username?.Trim();

        if (string.IsNullOrWhiteSpace(authorName))
            authorName = "Unknown";

        var authorId = dto.User?.Id ?? Guid.Empty;

        return new ChatMessageViewModel
        {
            Id = dto.Id,
            AuthorId = authorId,
            AuthorName = authorName,
            Content = dto.Content ?? string.Empty,
            SentAt = dto.CreatedAt,
            IsMine = authorId != Guid.Empty && authorId == UserId,
            Initial = UiTextHelper.GetInitial(authorName),
            AvatarColor = GetColor(dto.User?.Username ?? "?")
        };
    }

    private ChatMemberViewModel MapMember(UserDto dto)
    {
        var isYou = dto.Id == UserId;

        var displayName = string.IsNullOrWhiteSpace(dto.Fullname)
            ? dto.Username
            : dto.Fullname;

        if (isYou)
            displayName = $"{displayName} (You)";

        return new ChatMemberViewModel
        {
            Id = dto.Id,
            Username = dto.Username,
            DisplayName = displayName,
            PresenceStatus = GetPresenceStatus(dto.Presence?.LastActive),
            Initial = UiTextHelper.GetInitial(string.IsNullOrWhiteSpace(dto.Fullname) ? dto.Username : dto.Fullname),
            IsYou = isYou,
            AvatarColor = GetColor(dto.Username),
            LastActive = dto.Presence?.LastActive
        };
    }

    private static string GetPresenceStatus(DateTimeOffset? lastActive)
    {
        if (!lastActive.HasValue)
            return "Offline";

        var elapsed = DateTimeOffset.UtcNow - lastActive.Value;
        if (elapsed <= TimeSpan.FromMinutes(2))
            return "Online";

        return elapsed <= TimeSpan.FromMinutes(15) ? "Away" : "Offline";
    }

    private static string GetPresenceCssClass(ChatMemberViewModel member) =>
        GetPresenceStatus(member.LastActive).ToLowerInvariant();

    private async Task RefreshPresenceStatusesAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PresenceRefreshInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                _logger.LogTrace("Refreshing locally derived presence statuses");
                Members.ForEach(x => x.PresenceStatus = GetPresenceStatus(x.LastActive));
                Members.Sort(ChatMemberViewModel.Comparer.Instance);

                await HubConnection.InvokeAsync(GameHubConstants.SignalR.UpdatePresence);

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Component disposal stops the local presence refresh loop.
        }
    }

    private string? BuildMessagesNextCursor(CursorPage<MessageDto> page)
    {
        if (!string.IsNullOrWhiteSpace(page.Next))
            return page.Next;

        if (page.Items.Count == 0)
            return null;

        var oldestLoaded = Messages.OrderBy(x => x.SentAt).ThenBy(x => x.Id).FirstOrDefault();
        if (oldestLoaded is null)
            return null;

        return ChatMessageCursor.Cursor.Encode(oldestLoaded.SentAt, oldestLoaded.Id);
    }

    private string? BuildMembersNextCursor(CursorPage<UserDto> page)
    {
        if (!string.IsNullOrWhiteSpace(page.Next))
            return page.Next;

        if (page.Items.Count == 0)
            return null;

        var lastLoaded = Members
            .OrderByDescending(x => x.LastActive.HasValue)
            .ThenByDescending(x => x.LastActive)
            .ThenBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id)
            .LastOrDefault();

        if (lastLoaded is null)
            return null;

        return ChatParticipantCursor.Cursor.Encode(lastLoaded.LastActive, lastLoaded.Username, lastLoaded.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await _presenceRefreshCancellation.CancelAsync();

        if (_presenceRefreshTask is not null)
        {
            await _presenceRefreshTask;
        }

        _presenceRefreshCancellation.Dispose();

        foreach (var subscription in _hubSubscriptions)
        {
            subscription.Dispose();
        }

        await HubConnection.InvokeAsync(GameHubConstants.SignalR.LeaveChat, ChatId);
        await JS.InvokeVoidAsync("chatChannelPage.dispose");
        _dotNetRef?.Dispose();
    }
}
