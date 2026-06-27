using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public sealed class SettingsView : Form, ISettingsView
{
    private readonly CheckBox _autoStartCheckBox = new() { Text = "Start all enabled devices on controller start" };
    private readonly CheckBox _autoShutdownCheckBox = new() { Text = "Shutdown enabled devices on controller close" };
    private readonly NumericUpDown _delayNumeric = new() { Minimum = 0, Maximum = 60000, Increment = 250 };
    private readonly NumericUpDown _pingTimeoutNumeric = new() { Minimum = 1, Maximum = 60, Value = 5 };
    private readonly NumericUpDown _sshTimeoutNumeric = new() { Minimum = 1, Maximum = 120, Value = 10 };
    private readonly NumericUpDown _retryCountNumeric = new() { Minimum = 1, Maximum = 10, Value = 1 };
    private readonly NumericUpDown _defaultWolPortNumeric = new() { Minimum = 1, Maximum = 65535, Value = 9 };
    private readonly TextBox _defaultBroadcastAddressTextBox = new() { PlaceholderText = "255.255.255.255" };
    private readonly CheckBox _enableLogsCheckBox = new() { Text = "Enable logs" };
    private readonly NumericUpDown _retentionDaysNumeric = new() { Minimum = 1, Maximum = 365, Value = 30 };
    private readonly CheckBox _confirmManualShutdownCheckBox = new() { Text = "Confirm manual shutdown" };

    public SettingsView()
    {
        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ModernUi.AppBackground;
        Font = ModernUi.BodyFont();
        Width = 680;
        Height = 620;
        BuildLayout();
    }

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public SettingsViewModel Settings
    {
        get => new()
        {
            AutoStartDevicesOnControllerBoot = _autoStartCheckBox.Checked,
            AutoShutdownDevicesOnControllerShutdown = _autoShutdownCheckBox.Checked,
            DelayBetweenCommandsMs = (int)_delayNumeric.Value,
            PingTimeoutSeconds = (int)_pingTimeoutNumeric.Value,
            SshTimeoutSeconds = (int)_sshTimeoutNumeric.Value,
            RetryCount = (int)_retryCountNumeric.Value,
            DefaultWolPort = (int)_defaultWolPortNumeric.Value,
            DefaultBroadcastAddress = _defaultBroadcastAddressTextBox.Text.Trim(),
            EnableLogs = _enableLogsCheckBox.Checked,
            LogRetentionDays = (int)_retentionDaysNumeric.Value,
            ConfirmManualShutdown = _confirmManualShutdownCheckBox.Checked
        };
        set
        {
            _autoStartCheckBox.Checked = value.AutoStartDevicesOnControllerBoot;
            _autoShutdownCheckBox.Checked = value.AutoShutdownDevicesOnControllerShutdown;
            _delayNumeric.Value = value.DelayBetweenCommandsMs;
            _pingTimeoutNumeric.Value = value.PingTimeoutSeconds;
            _sshTimeoutNumeric.Value = value.SshTimeoutSeconds;
            _retryCountNumeric.Value = value.RetryCount;
            _defaultWolPortNumeric.Value = value.DefaultWolPort;
            _defaultBroadcastAddressTextBox.Text = value.DefaultBroadcastAddress;
            _enableLogsCheckBox.Checked = value.EnableLogs;
            _retentionDaysNumeric.Value = value.LogRetentionDays;
            _confirmManualShutdownCheckBox.Checked = value.ConfirmManualShutdown;
        }
    }

    public DialogResult ShowDialogView(IWin32Window owner) => ShowDialog(owner);

    public void CloseView(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = BackColor,
            RowCount = 3,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerCard = ModernUi.CreateCard(22);
        headerCard.Margin = new Padding(0, 0, 0, 14);
        headerCard.Controls.Add(new Label
        {
            Text = "System Settings",
            AutoSize = true,
            Font = ModernUi.TitleFont(18f),
            ForeColor = ModernUi.TextStrong
        });
        headerCard.Controls.Add(new Label
        {
            Text = "Tune automation, timeouts, logging, and controller behavior for a smoother operations workflow.",
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            ForeColor = ModernUi.TextMuted,
            Margin = new Padding(0, 10, 0, 0)
        });

        var contentCard = ModernUi.CreateCard(18);
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(content, "Delay Between Commands (ms)", _delayNumeric);
        AddRow(content, "Ping Timeout (sec)", _pingTimeoutNumeric);
        AddRow(content, "SSH Timeout (sec)", _sshTimeoutNumeric);
        AddRow(content, "Retry Count", _retryCountNumeric);
        AddRow(content, "Default WOL Port", _defaultWolPortNumeric);
        AddRow(content, "Default Broadcast", _defaultBroadcastAddressTextBox);
        AddRow(content, "Log Retention Days", _retentionDaysNumeric);

        foreach (CheckBox checkBox in new[] { _autoStartCheckBox, _autoShutdownCheckBox, _enableLogsCheckBox, _confirmManualShutdownCheckBox })
        {
            ModernUi.StyleCheckBox(checkBox);
            content.Controls.Add(new Label { AutoSize = true }, 0, content.RowCount);
            content.Controls.Add(checkBox, 1, content.RowCount);
            content.RowCount++;
        }

        contentCard.Controls.Add(ModernUi.CreateSectionLabel("Automation And Reliability"));
        contentCard.Controls.Add(content);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 58,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = BackColor
        };

        var saveButton = ModernUi.CreateButton("Save Settings", primary: true);
        var cancelButton = ModernUi.CreateButton("Cancel");
        saveButton.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
        cancelButton.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        root.Controls.Add(headerCard, 0, 0);
        root.Controls.Add(contentCard, 0, 1);
        root.Controls.Add(buttonsPanel, 0, 2);
        Controls.Add(root);
    }

    private static void AddRow(TableLayoutPanel table, string labelText, Control control)
    {
        table.Controls.Add(ModernUi.CreateFieldLabel(labelText), 0, table.RowCount);
        ModernUi.StyleInput(control);
        control.Dock = DockStyle.Top;
        table.Controls.Add(control, 1, table.RowCount);
        table.RowCount++;
    }
}
