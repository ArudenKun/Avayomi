using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avayomi.Messaging.Messages;
using Avayomi.ViewModels.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using ZLinq;

namespace Avayomi.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class MainViewModel : ViewModel, IRecipient<ChangePageMessage>
{
    public MainViewModel(IEnumerable<PageViewModel> pageViewModels)
    {
        Pages = new AvaloniaList<PageViewModel>(
            pageViewModels.AsValueEnumerable().OrderBy(x => x.Index).ToArray()
        );

        Page = Pages[0];
    }

    [ObservableProperty]
    public partial PageViewModel Page { get; set; }

    public IAvaloniaList<PageViewModel> Pages { get; }

    public void Receive(ChangePageMessage message)
    {
        var pageViewModel = Pages.FirstOrDefault(x => x.GetType() == message.ViewModelType);
        if (pageViewModel is null)
        {
            Logger.LogWarning("No matching page found for {Type}", message.ViewModelType);
            return;
        }
        Page = pageViewModel;
    }
}
