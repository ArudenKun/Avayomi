using System.Threading.Tasks;
using Avalonia.Markup.Declarative;
using PleasantUI;
using PleasantUI.ToolKit;
using R3.ObservableEvents;

[assembly: GenerateStaticEventObservables(typeof(TaskScheduler))]
[assembly: GenerateMarkupExtensionsForAssembly(typeof(PleasantTheme))]
[assembly: GenerateMarkupExtensionsForAssembly(typeof(PleasantDataGridTheme))]
[assembly: GenerateMarkupExtensionsForAssembly(typeof(PleasantUIToolKit))]
