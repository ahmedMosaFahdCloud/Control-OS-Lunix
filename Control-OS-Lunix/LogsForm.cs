namespace Control_OS_Lunix;

public sealed class LogsForm : Form
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

    public LogsForm(IEnumerable<string> lines)
    {
        Text = "Operation Logs";
        Width = 960;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        _logTextBox.Text = string.Join(Environment.NewLine, lines);
        Controls.Add(_logTextBox);
    }
}
