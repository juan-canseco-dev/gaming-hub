using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Shared.Themes;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace GameHub.Web.UI.Layout;

public partial class MainLayout
{
    private MudTheme? _theme = null;
    [Inject]
    public required NavigationManager NavManager { get; set; }
    [Inject]
    public required JwtAuthenticationStateProvider AuthProvider { get; set; }
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _theme = GameHubTheme.DefaultTheme;
    }

    protected override async Task OnInitializedAsync()
    {
        if (await AuthProvider.IsLoggedInAsync())
        {
            NavManager.NavigateTo("/home");
        }
    }
}
