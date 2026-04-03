using GameHub.Contracts.Channels;
using GameHub.Contracts.Chats;
using GameHub.Contracts.Notifications;
using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Features.Channels.Models;
using GameHub.Web.UI.Features.Channels.Services.Interfaces;
using GameHub.Web.UI.Shared.Constants;
using GameHub.Web.UI.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace GameHub.Web.UI.Features.Channels.Pages;

public partial class MyChannels : ComponentBase, IAsyncDisposable
{
    [CascadingParameter(Name = "HubConnection")]
    public required HubConnection HubConnection { get; set; }
    [Inject]
    public required NavigationManager NavigationManager { get; set; }
    [Inject]
    public required IChatService ChatService { get; set; }
    [Inject]
    public required ISnackbar Snackbar { get; set; }

    private List<ChatViewModel> _channels = new();
    private bool _isLoading = true;
    private bool _hasError = false;

    private async Task<int> GetUnreadCount(Guid chatId, int previousUnreadCount)
    {
        var result = await ChatService.GetUnreadMesasgesCount(chatId);
        return result.IsSuccess ? result.Value : previousUnreadCount;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        HubConnection.On<UserJoinedNotification>(GameHubConstants.SignalR.UserJoinedChat, async (notification) =>
        {
            var channel = _channels.FirstOrDefault(channel => channel.Id == notification.ChatId)!;
            var updatedParticipantsCount = notification.NumberOfParticipants;
            var unreadCount = await GetUnreadCount(notification.ChatId, channel.UnreadCount);

            channel.ParticipantsCount = updatedParticipantsCount;
            channel.UnreadCount = unreadCount;
            channel.LastMesagePreview = notification.Message.Content;
            channel.LastMessageAt = notification.Message.CreatedAt;

            _channels.Sort(ChatViewModel.Comparer.Instance);

            await InvokeAsync(StateHasChanged);
        });

        HubConnection.On<MessageNotification>(GameHubConstants.SignalR.MessageSent, async (notification) =>
        {
            var channel = _channels.FirstOrDefault(channel => channel.Id == notification.ChatId)!;
            var unreadCount = await GetUnreadCount(notification.ChatId, channel.UnreadCount);

            channel.UnreadCount = unreadCount;
            channel.LastMesagePreview = notification.Message.Content;
            channel.LastMessageAt = notification.Message.CreatedAt;

            _channels.Sort(ChatViewModel.Comparer.Instance);

            await InvokeAsync(StateHasChanged);
        });
    }

    private async Task LoadAsync()
    {
        _isLoading = true;
        _hasError = false;

        await Task.Delay(1500);

        var result = await ChatService.GetListAsync();

        if (result.IsFailure)
        {
            _hasError = true;

            Snackbar.Clear();
            Snackbar.Add(result.Error.Description, Severity.Error);

            _isLoading = false;
            return;
        }

        _channels = result.Value
            .Select(MapToVM)
            .Cast<ChatViewModel>()
            .ToList();

        _channels.Sort(ChatViewModel.Comparer.Instance);

        _isLoading = false;

        await InvokeAsync(StateHasChanged);

        await JoinChannels(_channels);
    }

    private ChatViewModel MapToVM(ChatDto chatDto)
    {
        return new ChatViewModel
        {
            Id = chatDto.Id,
            AvatarColor = AvatarColorHelper.GetColor(chatDto.Slug),
            ChatAlias = UiTextHelper.GetInitial(chatDto.Name),
            ChannelId = chatDto.ChannelId,
            Slug = chatDto.Slug,
            Name = chatDto.Name,
            Description = chatDto.Description,
            ParticipantsCount = chatDto.ParticipantsCount,
            LastMesagePreview = chatDto.LastMesagePreview,
            LastMessageAt = chatDto.LastMessageAt,
            UnreadCount = chatDto.UnreadCount
        };
    }

    private void OpenChannel(ChatViewModel channel)
    {
        NavigationManager.NavigateTo($"/channels/{channel.Id}");
    }

    private async Task JoinChannels(List<ChatViewModel> chats)
    {
        var chatIds = chats
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        var tasks = chatIds
            .Select(chatId => HubConnection.InvokeAsync(GameHubConstants.SignalR.JoinChat, chatId));

        await Task.WhenAll(tasks);
    }

    private async Task LeaveChannels(List<ChatViewModel> chats)
    {
        var chatIds = chats
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        var tasks = chatIds
            .Select(chatId => HubConnection.InvokeAsync(GameHubConstants.SignalR.LeaveChat, chatId));
        await Task.WhenAll(tasks);
    }


    public async ValueTask DisposeAsync()
    {
        await LeaveChannels(_channels);
    }
}
