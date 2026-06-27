namespace Control_OS_Lunix.UI.Views;

public interface ILogsView
{
    void SetLines(IReadOnlyList<string> lines);

    DialogResult ShowDialogView(IWin32Window owner);
}
