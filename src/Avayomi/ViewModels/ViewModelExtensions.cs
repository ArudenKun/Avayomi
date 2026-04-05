using System;

namespace Avayomi.ViewModels;

public static class ViewModelExtensions
{
    public static TDisposable AddTo<TDisposable>(this TDisposable disposable, ViewModel viewModel)
        where TDisposable : IDisposable
    {
        viewModel.Disposables.Add(disposable);
        return disposable;
    }
}
