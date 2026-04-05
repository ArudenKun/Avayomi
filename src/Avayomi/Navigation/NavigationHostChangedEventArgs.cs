using System;

namespace Avayomi.Navigation;

public sealed class NavigationHostChangedEventArgs : EventArgs
{
    public NavigationHostChangedEventArgs(object? content)
    {
        CurrentContent = content;
    }

    public object? CurrentContent { get; }
}
