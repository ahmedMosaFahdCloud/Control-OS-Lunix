namespace Control_OS_Lunix.UI.Views;

public sealed class LogsView : Form, ILogsView
{
    private readonly TextBox _logTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new Font("Consolas", 10)
    };

    public LogsView()
    {
        Text = "Operation Logs";
        Width = 960;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(_logTextBox);
    }

    public void SetLines(IReadOnlyList<string> lines)
    {
        _logTextBox.Text = string.Join(Environment.NewLine, lines);
    }

    public DialogResult ShowDialogView(IWin32Window owner) => ShowDialog(owner);
}
