using Avayomi.ViewModels.Components;

namespace Avayomi.Views.Components;

public partial class AnimeCardStatusButtonView : UserControl<AnimeCardStatusButtonViewModel>
{
    private bool _mouseOverRoot;

    public AnimeCardStatusButtonView()
    {
        InitializeComponent();

        MainButton.PointerEntered += (_, _) => ShowStatusButtons();
        MainButton.PointerExited += (_, _) => HideStatusButtons();
        Root.PointerExited += (_, _) => RootPointerExited();
        Root.PointerEntered += (_, _) => RootPointerEnter();
    }

    private void ShowStatusButtons()
    {
        PlannedToWatchButton.Classes.Add("visible");
        WatchingButton.Classes.Add("visible");
        CompletedButton.Classes.Add("visible");
        RemoveButton.Classes.Add("visible");

        PlannedToWatchButton.IsVisible = true;
        WatchingButton.IsVisible = true;
        CompletedButton.IsVisible = true;
        RemoveButton.IsVisible = true;
    }

    private void HideStatusButtons()
    {
        if (!_mouseOverRoot)
        {
            PlannedToWatchButton.Classes.Remove("visible");
            WatchingButton.Classes.Remove("visible");
            CompletedButton.Classes.Remove("visible");
            RemoveButton.Classes.Remove("visible");
        }
    }

    private void RootPointerEnter()
    {
        _mouseOverRoot = true;
    }

    private void RootPointerExited()
    {
        _mouseOverRoot = false;
        HideStatusButtons();
    }
}
