using Control_OS_Lunix.Model;

namespace Control_OS_Lunix;

public sealed class SettingsDialogForm : Form
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

    public SettingsDialogForm(GlobalSettings settings)
    {
        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 520;
        Height = 460;

        BuildLayout();
        LoadValues(settings);
    }

    public GlobalSettings? Result { get; private set; }

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
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };

        saveButton.Click += SaveButton_Click;
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        Controls.Add(container);
        Controls.Add(buttonsPanel);
    }

    private void LoadValues(GlobalSettings settings)
    {
        _autoStartCheckBox.Checked = settings.AutoStartDevicesOnControllerBoot;
        _autoShutdownCheckBox.Checked = settings.AutoShutdownDevicesOnControllerShutdown;
        _delayNumeric.Value = settings.DelayBetweenCommandsMs;
        _pingTimeoutNumeric.Value = settings.PingTimeoutSeconds;
        _sshTimeoutNumeric.Value = settings.SshTimeoutSeconds;
        _retryCountNumeric.Value = settings.RetryCount;
        _defaultWolPortNumeric.Value = settings.DefaultWolPort;
        _defaultBroadcastAddressTextBox.Text = settings.DefaultBroadcastAddress;
        _enableLogsCheckBox.Checked = settings.EnableLogs;
        _retentionDaysNumeric.Value = settings.LogRetentionDays;
        _confirmManualShutdownCheckBox.Checked = settings.ConfirmManualShutdown;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        Result = new GlobalSettings
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

        DialogResult = DialogResult.OK;
        Close();
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
