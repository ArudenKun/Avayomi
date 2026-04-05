using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Lucide.Avalonia;

namespace Avayomi.ViewModels.Pages;

public abstract partial class PageViewModel : ViewModel
{
    protected PageViewModel()
    {
        DisplayName = GetType()
            .Name.Replace("PageViewModel", string.Empty)
            .Humanize(LetterCasing.Title);
    }

    /// <summary>
    /// The index of the page.
    /// </summary>
    public abstract int Index { get; }

    /// <summary>
    /// The display name of the page.
    /// </summary>
    public virtual string DisplayName { get; }

    /// <summary>
    /// The icon of the page.
    /// </summary>
    public abstract LucideIconKind IconKind { get; }

    /// <summary>
    /// The visibility of the page on the side menu.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVisibleOnSideMenu { get; protected set; } = true;

    /// <summary>
    /// Set to true to auto hide the page on the side menu.
    /// </summary>
    public virtual bool AutoHideOnSideMenu => false;

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
