using GameHub.Web.UI.Features.Auth.Models;
using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Infrastructure.Options;
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

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = _hubConnection.TryInitialize(
            AuthProvider,
            ApiSettingsOptions.Value
         );

        await _hubConnection.StartAsync();
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

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
