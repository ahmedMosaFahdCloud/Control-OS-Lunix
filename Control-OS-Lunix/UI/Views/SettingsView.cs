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
        Width = 520;
        Height = 460;
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
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(container, "Delay Between Commands (ms)", _delayNumeric);
        AddRow(container, "Ping Timeout (sec)", _pingTimeoutNumeric);
        AddRow(container, "SSH Timeout (sec)", _sshTimeoutNumeric);
        AddRow(container, "Retry Count", _retryCountNumeric);
        AddRow(container, "Default WOL Port", _defaultWolPortNumeric);
        AddRow(container, "Default Broadcast", _defaultBroadcastAddressTextBox);
        AddRow(container, "Log Retention Days", _retentionDaysNumeric);
        container.Controls.Add(_autoStartCheckBox, 1, container.RowCount++);
        container.Controls.Add(_autoShutdownCheckBox, 1, container.RowCount++);
        container.Controls.Add(_enableLogsCheckBox, 1, container.RowCount++);
        container.Controls.Add(_confirmManualShutdownCheckBox, 1, container.RowCount++);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(16, 8, 16, 8)
        };

        var saveButton = new Button { Text = "Save", AutoSize = true };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true };
        saveButton.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
        cancelButton.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        Controls.Add(container);
        Controls.Add(buttonsPanel);
    }

    private static void AddRow(TableLayoutPanel table, string labelText, Control control)
    {
        table.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0)
        }, 0, table.RowCount);
        control.Dock = DockStyle.Top;
        table.Controls.Add(control, 1, table.RowCount);
        table.RowCount++;
    }
}
