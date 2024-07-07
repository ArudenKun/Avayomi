using System.ComponentModel;

namespace Generator.Interfaces;

public interface IView<TViewModel>
    where TViewModel : INotifyPropertyChanged
{
    TViewModel ViewModel { get; init; }
}
