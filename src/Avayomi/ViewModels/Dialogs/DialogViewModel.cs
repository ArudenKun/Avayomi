using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;

namespace Avayomi.ViewModels.Dialogs;

[PublicAPI]
public abstract class DialogViewModel : DialogViewModel<bool>;

[PublicAPI]
public abstract partial class DialogViewModel<TResult> : ViewModel
{
    private bool _isResultSet;

    // protected DialogViewModel()
    // {
    //     Dialog = new SukiDialog();
    // }
    //
    // protected ISukiDialog Dialog { get; private set; }

    public TResult? Result { get; private set; }
    public TaskCompletionSource<bool> Completion { get; private set; } = new();

    /// <summary>
    /// Gets the title of the dialog.
    /// </summary>
    public virtual string Title => string.Empty;

    public override void OnLoaded()
    {
        base.OnLoaded();

        Reset();
    }

    protected virtual bool CanExecuteClose() => true;

    [RelayCommand(CanExecute = nameof(CanExecuteClose))]
    protected void Close(object? result = null)
    {
        Result = result is TResult t ? t : default;
        Completion.SetResult(true);
        _isResultSet = true;
        // Dialog.Dismiss();
    }

    // public void SetDialog(ISukiDialog dialog)
    // {
    //     Dialog = dialog;
    // }

    protected void Reset()
    {
        if (!_isResultSet)
            return;

        Completion = new TaskCompletionSource<bool>(false);
        Result = default;
        _isResultSet = false;
    }
}
