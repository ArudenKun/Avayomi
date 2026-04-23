using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Markup.Declarative;
using Flowery;
using HeroIconsAvalonia.Controls;
using R3.ObservableEvents;

[assembly: GenerateStaticEventObservables(typeof(TaskScheduler))]
[assembly: GenerateMarkupExtensionsForAssembly(typeof(DaisyUITheme))]
[assembly: GenerateMarkupExtensionsForAssembly(typeof(AdvancedImage))]
[assembly: GenerateMarkupExtensionsForAssembly(typeof(HeroIcon))]
