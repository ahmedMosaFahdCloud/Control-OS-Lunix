using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public interface ISettingsView
{
    event EventHandler SaveRequested;
    event EventHandler CancelRequested;

    SettingsViewModel Settings { get; set; }

    DialogResult ShowDialogView(IWin32Window owner);

    void CloseView(DialogResult result);
}
