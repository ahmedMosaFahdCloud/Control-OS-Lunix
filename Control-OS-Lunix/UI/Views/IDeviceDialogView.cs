using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public interface IDeviceDialogView
{
    event EventHandler SaveRequested;
    event EventHandler CancelRequested;

    DeviceEditViewModel FormData { get; set; }

    string ViewTitle { set; }

    DialogResult ShowDialogView(IWin32Window owner);

    void CloseView(DialogResult result);

    void ShowWarning(string message, string title);
}
