using GameHub.Contracts.Channels;
using GameHub.Contracts.Notifications;
using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Features.Channels.Services.Interfaces;
using GameHub.Web.UI.Shared.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace GameHub.Web.UI.Features.Channels.Pages;

public partial class Channels : ComponentBase, IAsyncDisposable
{

    [CascadingParameter(Name = "HubConnection")]
    public required HubConnection HubConnection { get; set; }

    [Inject]
    public required JwtAuthenticationStateProvider AuthProvider { get; set; }
    [Inject]
    public required IChannelsService ChannelsService { get; set; }
    [Inject]
    public required NavigationManager NavManager { get; set; }
    [Inject]
    public required ISnackbar Snackbar { get; set; }

    private List<ChannelDto> ChannelList { get; set; } = new();
    private bool IsLoading = true;

    // Track loading per channel
    private readonly HashSet<Guid> _joiningChannels = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadChannelsAsync();
        HubConnection.On<UserJoinedNotification>(GameHubConstants.SignalR.UserJoinedChat, (notification) =>
        {
            var channel = ChannelList.FirstOrDefault(channel => channel.ChatId == notification.ChatId)!;
            var updatedParticipantsCount = notification.NumberOfParticipants;
            channel = channel with { ParticipantsCount = updatedParticipantsCount };

            var index = ChannelList.FindIndex(c => c.ChatId == channel.ChatId);
            if (index >= 0)
            {
                ChannelList[index] = channel;
            }

            StateHasChanged();
        });
    }

    private async Task LoadChannelsAsync()
    {
        IsLoading = true;
        var result = await ChannelsService.GetListAsync();

        if (result.IsFailure)
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
            IsLoading = false;
            return;
        }

        ChannelList = result.Value;
        IsLoading = false;
        await InvokeAsync(StateHasChanged);

        await JoinChannels(ChannelList);
    }

    private async Task JoinChannels(List<ChannelDto> channelList)
    {
        var channelIds = channelList
            .Select(x => x.ChatId)
            .Distinct()
            .ToList();

        var tasks = channelIds
            .Select(chatId => HubConnection.InvokeAsync(GameHubConstants.SignalR.JoinChat, chatId));

        await Task.WhenAll(tasks);
    }

    private async Task LeaveChannels(List<ChannelDto> channelList)
    {
        var channelIds = channelList
            .Select(x => x.ChatId)
            .Distinct()
            .ToList();
        var tasks = channelIds
            .Select(chatId => HubConnection.InvokeAsync(GameHubConstants.SignalR.LeaveChat, chatId));
        await Task.WhenAll(tasks);
    }

    private bool IsJoining(Guid chatId) => _joiningChannels.Contains(chatId);

    private async Task HandleChannelAction(ChannelDto channel)
    {
        if (IsJoining(channel.ChatId))
            return;

        if (channel.IsJoined)
        {
            NavManager.NavigateTo($"/channels/{channel.ChatId}");
            return;
        }

        _joiningChannels.Add(channel.ChatId);
        StateHasChanged();
        await Task.Delay(2000);
        var result = await ChannelsService.JoinAsync(channel.ChatId);

        _joiningChannels.Remove(channel.ChatId);

        if (result.IsFailure)
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
            StateHasChanged();
            return;
        }

        channel = channel with { IsJoined = true };

        var index = ChannelList.FindIndex(c => c.ChatId == channel.ChatId);
        if (index >= 0)
        {
            ChannelList[index] = channel;
        }

        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        await LeaveChannels(ChannelList);
    }
}
