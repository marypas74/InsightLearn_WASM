using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using InsightLearn.WebAssembly.Services.Http;
using Blazored.Toast.Services;

namespace InsightLearn.WebAssembly.Components;

/// <summary>
/// Base class for admin pages with common functionality
/// </summary>
public abstract class AdminPageBase : ComponentBase
{
    [CascadingParameter]
    protected Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    [Inject]
    protected IToastService Toast { get; set; } = default!;

    [Inject]
    protected IApiClient ApiClient { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected string? ErrorMessage { get; set; }
    protected string CurrentUserName { get; set; } = "Admin";

    protected override async Task OnInitializedAsync()
    {
        await CheckAuthorization();
        await LoadUserInfo();
        await LoadData();
    }

    protected virtual async Task CheckAuthorization()
    {
        if (AuthenticationStateTask != null)
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;

            if (!user.IsInRole("Admin"))
            {
                Navigation.NavigateTo("/unauthorized", forceLoad: true);
            }
        }
    }

    protected virtual async Task LoadUserInfo()
    {
        if (AuthenticationStateTask != null)
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;
            CurrentUserName = user.Identity?.Name ?? "Admin";
        }
    }

    protected abstract Task LoadData();

    protected void ShowError(string message)
    {
        ErrorMessage = message;
        Toast.ShowError(message);
    }

    protected void ShowSuccess(string message)
    {
        Toast.ShowSuccess(message);
    }
}
