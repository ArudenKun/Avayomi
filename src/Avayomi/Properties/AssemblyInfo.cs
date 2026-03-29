using System.Threading.Tasks;
using Avalonia.Metadata;
using R3.ObservableEvents;

[assembly: GenerateStaticEventObservables(typeof(TaskScheduler))]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.Converters")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.Navigation")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.Utilities")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.ViewModels")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.ViewModels.Components")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.ViewModels.Dialogs")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.ViewModels.Pages")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.Views")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.Views.Components")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.Views.Dialogs")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/avayomi", "Avayomi.Controls")]
[assembly: XmlnsPrefix("https://github.com/arudenkun/avayomi", "avayomi")]
