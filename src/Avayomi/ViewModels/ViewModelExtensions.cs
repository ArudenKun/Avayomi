using System;
using R3;
using Volo.Abp.Data;

namespace Avayomi.ViewModels;

public static class ViewModelExtensions
{
    public static TDisposable AddTo<TDisposable>(this TDisposable disposable, ViewModel viewModel)
        where TDisposable : IDisposable
    {
        if (viewModel.GetProperty("Disposables") is not CompositeDisposable disposables)
        {
            disposables = [];
        }

        disposables.Add(disposable);
        viewModel.SetProperty("Disposables", disposables);
        return disposable;
    }
}
