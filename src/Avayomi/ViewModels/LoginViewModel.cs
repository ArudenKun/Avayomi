using System;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncNavigation;
using AsyncNavigation.Core;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avayomi.Extensions;
using Avayomi.Services;
using Avayomi.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;

namespace Avayomi.ViewModels;

public sealed partial class LoginViewModel : ViewModel
{
    private readonly TopLevel _topLevel;
    private readonly IAniListService _aniListService;

    private const string RedirectUrl = "http://127.0.0.1/avayomi";
    private const string ClientId = "38430";
    private const string ResponseType = "token";

    public LoginViewModel(TopLevel topLevel, IAniListService aniListService)
    {
        _topLevel = topLevel;
        _aniListService = aniListService;
    }

    [ObservableProperty]
    public partial bool RememberMe { get; set; }

    public override async Task OnNavigatedToAsync(NavigationContext context)
    {
        await base.OnNavigatedToAsync(context);

        var authenticated = _aniListService.IsAuthenticated;
        if (authenticated)
        {
            RegionManager.RequestNavigate<MainView>(Regions.Main);
        }
    }

    [RelayCommand]
    private async Task LoginAsync() =>
        await SetBusyAsync(async () =>
        {
            try
            {
                var requestUrl = new RequestUrl("https://anilist.co/api/v2/oauth/authorize");
                var startUrl = requestUrl.CreateAuthorizeUrl(ClientId, ResponseType);
                var result = await WebAuthenticationBroker.AuthenticateAsync(
                    _topLevel,
                    new WebAuthenticatorOptions(new Uri(startUrl), new Uri(RedirectUrl))
                );
                var authorizeResponse = new AuthorizeResponse(result.CallbackUri.AbsoluteUri);
                var accessToken = authorizeResponse.AccessToken;
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await _aniListService.AuthenticateAsync(accessToken, RememberMe);
                    if (_aniListService.IsAuthenticated)
                    {
                        await RegionManager.RequestNavigateAsync<MainView>(
                            Regions.Main,
                            new NavigationParameters
                            {
                                { "Auth", await _aniListService.GetAuthenticatedUserAsync() },
                            }
                        );
                    }
                    return;
                }

                await _aniListService.LogoutAsync();
                //ToastService.ShowToast(
                //    NotificationType.Warning,
                //    "Login Failed",
                //    "Login was cancelled or an error occured while logging in"
                //);
            }
            catch (TaskCanceledException)
            {
                Logger.LogInformation("Login was cancelled by the user.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred during login.");
            }
        });
}
