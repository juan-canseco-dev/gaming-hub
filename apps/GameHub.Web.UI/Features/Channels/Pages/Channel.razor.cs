using GameHub.Abstractions.Pagination;
using GameHub.Contracts.Chats;
using GameHub.Contracts.Profile;
using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Features.Channels.Models;
using GameHub.Web.UI.Features.Channels.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;


namespace GameHub.Web.UI.Features.Channels.Pages;

public partial class Channel : ComponentBase
{
    [Parameter]
    public Guid ChatId { get; set; }

    [Inject]
    public required JwtAuthenticationStateProvider AuthProvider { get; set; }
    [Inject]
    public required NavigationManager NavigationManager { get; set; }
    [Inject]
    public required IChatService ChatService { get; set; }
    [Inject]
    public required IChannelsService ChannelsService { get; set; }
    [Inject]
    public required IJSRuntime JS { get; set; }
    [Inject]
    public required ISnackbar Snackbar { get; set; }

    private const int MessagesPageSize = 30;
    private const int MembersPageSize = 30;



    private Guid UserId = Guid.Empty;

    private string ChannelName = string.Empty;
    private string ChannelDescription = string.Empty;
    private int ParticipantsCount = 0;

    private string _messageText = string.Empty;
    private string? _lastFailedMessageText;

    private bool _isHeaderLoading = true;
    private bool _isMessagesLoading = true;
    private bool _isMembersLoading = true;
    private bool _isLoadingMoreMessages;
    private bool _isLoadingMoreMembers;
    private bool _isSendingMessage;

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

    private ElementReference _messagesContainerRef;
    private ElementReference _messagesTopSentinelRef;
    private ElementReference _messagesBottomAnchorRef;
    private ElementReference _membersContainerRef;
    private ElementReference _membersBottomSentinelRef;

    private DotNetObjectReference<Channel>? _dotNetRef;
    private bool _observersInitialized;
    private bool _initialMessagesScrolled;

    private bool CanSendMessage =>
        ChatId != Guid.Empty &&
        !string.IsNullOrWhiteSpace(_messageText);

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

        await LoadAllAsync();
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
            await ScrollMessagesToBottomAsync();
        }
    }

    private async Task LoadAllAsync()
    {
        await LoadHeaderAsync();
        await LoadInitialMessagesAsync();
        await LoadInitialMembersAsync();
    }

    private async Task LoadHeaderAsync()
    {
        _isHeaderLoading = true;
        _headerError = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ChatService.GetByIdAsync(ChatId);

            if (result.IsFailure)
            {
                _headerError = result.Error.Description;
                _isHeaderLoading = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            var chat = result.Value;
            ChannelName = chat.Slug;
            ChannelDescription = chat.Description;
            ParticipantsCount = chat.ParticipantsCount;
        }
        catch
        {
            _headerError = "Failed to load channel header.";
        }

        _isHeaderLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadInitialMessagesAsync()
    {
        _isMessagesLoading = true;
        _messagesError = null;
        _messagesPagingError = null;
        await InvokeAsync(StateHasChanged);

        try
        {
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
        }
        catch
        {
            _messagesError = "Failed to load messages.";
        }

        _isMessagesLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadInitialMembersAsync()
    {
        _isMembersLoading = true;
        _membersError = null;
        _membersPagingError = null;
        await InvokeAsync(StateHasChanged);

        try
        {
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
        }
        catch
        {
            _membersError = "Failed to load members.";
        }

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

        try
        {
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
        }
        catch
        {
            _membersPagingError = "Failed to load more members.";
        }

        _isLoadingMoreMembers = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SendMessageAsync()
    {
        await SendMessageCoreAsync(_messageText);
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

        try
        {
            var request = new SendMessageRequest(ChatId, normalizedContent);
            var result = await ChatService.SendMessageAsync(request);

            if (result.IsFailure)
            {
                _sendMessageError = result.Error.Description;
                _isSendingMessage = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            _messageText = string.Empty;
            _lastFailedMessageText = null;

            await LoadInitialMessagesAsync();
            await ScrollMessagesToBottomAsync();
        }
        catch
        {
            _sendMessageError = "Failed to send message.";
        }

        _isSendingMessage = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ScrollMessagesToBottomAsync()
    {
        await JS.InvokeVoidAsync("chatChannelPage.scrollToBottom", _messagesContainerRef);
    }

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
            .Where(x => x is not null)
            .Cast<ChatMessageViewModel>()
            .ToList();

        foreach (var item in incoming)
        {
            if (Messages.Any(x => x.Id == item.Id))
                continue;

            Messages.Add(item);
        }

        Messages.Sort(ChatMessageViewModel.Comparer.Instance);
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

    private ChatMessageViewModel? MapMessage(MessageDto dto)
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
            Initial = GetInitial(authorName)
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
            Initial = GetInitial(string.IsNullOrWhiteSpace(dto.Fullname) ? dto.Username : dto.Fullname),
            IsYou = isYou
        };
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
            .OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id)
            .LastOrDefault();

        if (lastLoaded is null)
            return null;

        return ChatParticipantCursor.Cursor.Encode(lastLoaded.Username, lastLoaded.Id);
    }

    private static string GetInitial(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        return value.Trim()[0].ToString().ToUpperInvariant();
    }

    private static string FormatMessageTime(DateTimeOffset value)
    {
        return value.LocalDateTime.ToString("dd/MM/yyyy hh:mm tt");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("chatChannelPage.dispose");
        }
        catch
        {

        }

        _dotNetRef?.Dispose();
    }


}

