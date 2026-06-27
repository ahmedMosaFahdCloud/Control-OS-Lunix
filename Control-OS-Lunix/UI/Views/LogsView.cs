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
        BackColor = ModernUi.AppBackground;
        Font = ModernUi.BodyFont();
        Width = 960;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        BuildLayout();
    }

    public void SetLines(IReadOnlyList<string> lines)
    {
        _logTextBox.Text = string.Join(Environment.NewLine, lines);
    }

    public DialogResult ShowDialogView(IWin32Window owner) => ShowDialog(owner);

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(20),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var headerCard = ModernUi.CreateCard(22);
        headerCard.Margin = new Padding(0, 0, 0, 14);
        headerCard.Controls.Add(new Label
        {
            Text = "Operation Logs",
            AutoSize = true,
            Font = ModernUi.TitleFont(18f),
            ForeColor = ModernUi.TextStrong
        });
        headerCard.Controls.Add(new Label
        {
            Text = "Review recent controller, startup, shutdown, and device operation activity in a readable console-style view.",
            AutoSize = true,
            MaximumSize = new Size(780, 0),
            ForeColor = ModernUi.TextMuted,
            Margin = new Padding(0, 10, 0, 0)
        });

        var logCard = ModernUi.CreateCard(14);
        _logTextBox.BackColor = Color.FromArgb(15, 23, 42);
        _logTextBox.ForeColor = Color.FromArgb(226, 232, 240);
        _logTextBox.BorderStyle = BorderStyle.None;
        _logTextBox.Padding = new Padding(12);
        logCard.Controls.Add(_logTextBox);

        root.Controls.Add(headerCard, 0, 0);
        root.Controls.Add(logCard, 0, 1);
        Controls.Add(root);
    }
}
