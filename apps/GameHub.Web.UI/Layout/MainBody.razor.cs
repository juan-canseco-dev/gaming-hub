using GameHub.Web.UI.Features.Auth.Models;
using GameHub.Web.UI.Features.Auth.State;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameHub.Web.UI.Layout;

public partial class MainBody
{
    [Parameter]
    public required RenderFragment ChildContent { get; set; }
    [Inject]
    public required JwtAuthenticationStateProvider AuthProvider { get; set; }
    [Inject]
    public required NavigationManager NavManager { get; set; }
    [Inject]
    public required IDialogService DialogService { get; set; }
    private char? FirstLetterOfName { get; set; }
    private string? Fullname { get; set; }
    private string? UserName { get; set; }
    private UserDetails? UserDetails { get; set;  }
    
    private bool _drawerOpen = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserDataAsync();
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
}
