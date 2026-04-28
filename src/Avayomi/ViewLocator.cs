using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avayomi.ViewModels;
using Avayomi.Views;
using ServiceScan.SourceGenerator;
using Volo.Abp.DependencyInjection;

namespace Avayomi;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
public sealed partial class ViewLocator : IDataTemplate, ISingletonDependency
{
    private readonly ReadOnlyDictionary<Type, Func<ViewModel, Control>> _viewFactory;

    public ViewLocator()
    {
        _viewFactory = GetViewDefinitions()
            .ToDictionary(x => x.ViewModelType, x => x.ViewFactory)
            .AsReadOnly();
    }

    public TView CreateView<TView>(ViewModel viewModel)
        where TView : Control
    {
        return (TView)CreateView(viewModel);
    }

    public Control CreateView(ViewModel viewModel)
    {
        var viewModelType = viewModel.GetType();
        if (!_viewFactory.TryGetValue(viewModelType, out var factory))
        {
            return CreateText($"Could not find view for {viewModelType.FullName}");
        }

        var view = factory(viewModel);
        return view;
    }

    Control ITemplate<object?, Control?>.Build(object? data)
    {
        if (data is ViewModel viewModel)
        {
            return CreateView(viewModel);
        }

        return CreateText($"Could not find view for {data?.GetType().FullName}");
    }

    public bool Match(object? data) => data is ViewModel;

    [ScanForTypes(
        AssignableTo = typeof(UserControl<>),
        Handler = nameof(GetViewDefinitionsHandler)
    )]
    [ScanForTypes(
        AssignableTo = typeof(PleasantWindow<>),
        Handler = nameof(GetViewDefinitionsHandler)
    )]
    [ScanForTypes(AssignableTo = typeof(Window<>), Handler = nameof(GetViewDefinitionsHandler))]
    private static partial ViewDefinitions[] GetViewDefinitions();

    private static ViewDefinitions GetViewDefinitionsHandler<TView, TViewModel>()
        where TView : Control, new()
        where TViewModel : ViewModel =>
        new(typeof(TViewModel), viewModel => new TView { DataContext = viewModel });

    private static TextBlock CreateText(string text) => new() { Text = text };

    private sealed record ViewDefinitions(Type ViewModelType, Func<ViewModel, Control> ViewFactory);
}
