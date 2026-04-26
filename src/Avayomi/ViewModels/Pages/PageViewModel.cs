using System.Threading.Tasks;
using AsyncNavigation;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;

namespace Avayomi.ViewModels.Pages;

public abstract partial class PageViewModel : ViewModel
{
    protected PageViewModel()
    {
        DisplayName = GetType()
            .Name.Replace("PageViewModel", string.Empty)
            .Humanize(LetterCasing.Title);
    }

    protected bool AutoCleanup { get; set; } = true;

    /// <summary>
    /// The display name of the page.
    /// </summary>
    public virtual string DisplayName { get; }

    /// <summary>
    /// The visibility of the page on the side menu.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVisibleOnSideMenu { get; protected set; } = true;

    /// <summary>
    /// Set to true to auto hide the page on the side menu.
    /// </summary>
    public virtual bool AutoHideOnSideMenu => false;

    public override Task OnNavigatedFromAsync(NavigationContext context)
    {
        base.OnNavigatedFromAsync(context);

        if (AutoCleanup)
        {
            RequestUnloadAsync();
        }

        return Task.CompletedTask;
    }

    public override void OnLoaded()
    {
        if (AutoHideOnSideMenu)
        {
            IsVisibleOnSideMenu = true;
        }

        base.OnLoaded();
    }

    public override void OnUnloaded()
    {
        if (AutoHideOnSideMenu)
        {
            IsVisibleOnSideMenu = false;
        }

        base.OnUnloaded();
    }
}
