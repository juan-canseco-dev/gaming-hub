using GameHub.Web.UI.Features.Auth.Models;
using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Infrastructure.Options;
using GameHub.Web.UI.Shared.Constants;
using GameHub.Web.UI.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace GameHub.Web.UI.Layout;

public partial class MainBody : IAsyncDisposable
{
    [Parameter]
    public required RenderFragment ChildContent { get; set; }
    [Inject]
    public required JwtAuthenticationStateProvider AuthProvider { get; set; }
    [Inject]
    public required IOptions<ApiSettings> ApiSettingsOptions { get; set; }
    [Inject]
    public required NavigationManager NavManager { get; set; }
    [Inject]
    public required IDialogService DialogService { get; set; }
    private char? FirstLetterOfName { get; set; }
    private string? Fullname { get; set; }
    private string? UserName { get; set; }
    private UserDetails? UserDetails { get; set;  }
    
    private bool _drawerOpen = true;

    private HubConnection _hubConnection = null!;
    private readonly CancellationTokenSource _presenceCancellation = new();
    private Task? _presenceHeartbeatTask;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = _hubConnection.TryInitialize(
            AuthProvider,
            ApiSettingsOptions.Value
         );

        await _hubConnection.StartAsync();
        _hubConnection.Reconnected += OnHubReconnectedAsync;
        _presenceHeartbeatTask = RunPresenceHeartbeatAsync(_presenceCancellation.Token);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadUserDataAsync();
        }
    }

    private async Task LoadUserDataAsync()
    {
        UserDetails = await AuthProvider.GetAuthenticatedUserDetailsAsync();
        Fullname = UserDetails?.Fullname;
        FirstLetterOfName = !string.IsNullOrEmpty(Fullname) ? Fullname[0] : null;
        UserName = UserDetails?.UserName;
    }

    public async void OpenLogoutDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<LogoutDialog>("Log out", options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LogOutAsync();
        }
    }

    private async Task LogOutAsync()
    {
        await AuthProvider.LogOutAsync();
        NavManager.NavigateTo("/auth/login", true);
    }

    private async Task RunPresenceHeartbeatAsync(CancellationToken cancellationToken)
    {
        await TryUpdatePresenceAsync(cancellationToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(45));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await TryUpdatePresenceAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private Task OnHubReconnectedAsync(string? connectionId) =>
        TryUpdatePresenceAsync(_presenceCancellation.Token);

    private async Task TryUpdatePresenceAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.InvokeAsync(
                GameHubConstants.SignalR.UpdatePresence,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            // Automatic reconnect and the next heartbeat recover transient failures.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _hubConnection.Reconnected -= OnHubReconnectedAsync;
        _presenceCancellation.Cancel();
        if (_presenceHeartbeatTask is not null)
        {
            await _presenceHeartbeatTask;
        }
        _presenceCancellation.Dispose();
        await _hubConnection.DisposeAsync();
    }
}
