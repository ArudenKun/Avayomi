using Generator.Metadata.CopyCode;

namespace Generator.Interfaces;

[Copy]
public interface IView<TViewModel>
    where TViewModel : System.ComponentModel.INotifyPropertyChanged
{
    TViewModel ViewModel { get; init; }
}
