using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.UI.ViewModels;

namespace Control_OS_Lunix.UI.Views;

public sealed class DeviceDialogView : Form, IDeviceDialogView
{
    private readonly TextBox _nameTextBox = new() { PlaceholderText = "Ubuntu Server 01" };
    private readonly TextBox _ipAddressTextBox = new() { PlaceholderText = "192.168.1.18" };
    private readonly TextBox _macAddressTextBox = new() { PlaceholderText = "AA:BB:CC:DD:EE:FF" };
    private readonly TextBox _broadcastAddressTextBox = new() { PlaceholderText = "192.168.1.255" };
    private readonly NumericUpDown _wolPortNumeric = new() { Minimum = 1, Maximum = 65535, Value = 9 };
    private readonly TextBox _sshHostTextBox = new() { PlaceholderText = "192.168.1.18" };
    private readonly NumericUpDown _sshPortNumeric = new() { Minimum = 1, Maximum = 65535, Value = 22 };
    private readonly TextBox _sshUsernameTextBox = new();
    private readonly TextBox _sshPasswordTextBox = new() { UseSystemPasswordChar = true };
    private readonly ComboBox _operatingSystemComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _autoStartCheckBox = new() { Text = "Auto start enabled" };
    private readonly CheckBox _autoShutdownCheckBox = new() { Text = "Auto shutdown enabled" };
    private readonly CheckBox _manualControlCheckBox = new() { Text = "Manual control enabled" };
    private readonly CheckBox _activeCheckBox = new() { Text = "Active" };
    private readonly NumericUpDown _timeoutSecondsNumeric = new() { Minimum = 1, Maximum = 120, Value = 15 };
    private readonly NumericUpDown _retryCountNumeric = new() { Minimum = 1, Maximum = 10, Value = 1 };
    private readonly TextBox _descriptionTextBox = new() { Multiline = true, Height = 72, ScrollBars = ScrollBars.Vertical };

    public DeviceDialogView()
    {
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 560;
        Height = 700;
        _operatingSystemComboBox.DataSource = Enum.GetValues<DeviceOperatingSystemType>();
        BuildLayout();
    }

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public DeviceEditViewModel FormData
    {
        get => new()
        {
            DeviceId = _tagDeviceId,
            Name = _nameTextBox.Text.Trim(),
            IpAddress = _ipAddressTextBox.Text.Trim(),
            MacAddress = _macAddressTextBox.Text.Trim(),
            BroadcastAddress = _broadcastAddressTextBox.Text.Trim(),
            WolPort = (int)_wolPortNumeric.Value,
            SshHost = _sshHostTextBox.Text.Trim(),
            SshPort = (int)_sshPortNumeric.Value,
            SshUsername = _sshUsernameTextBox.Text.Trim(),
            SshPassword = _sshPasswordTextBox.Text,
            OperatingSystemType = (DeviceOperatingSystemType)_operatingSystemComboBox.SelectedItem!,
            AutoStartEnabled = _autoStartCheckBox.Checked,
            AutoShutdownEnabled = _autoShutdownCheckBox.Checked,
            ManualControlEnabled = _manualControlCheckBox.Checked,
            IsActive = _activeCheckBox.Checked,
            TimeoutSeconds = (int)_timeoutSecondsNumeric.Value,
            RetryCount = (int)_retryCountNumeric.Value,
            Description = _descriptionTextBox.Text.Trim(),
            CreatedDateUtc = _createdDateUtc,
            LastUpdatedDateUtc = _lastUpdatedDateUtc,
            LastKnownStatus = _lastKnownStatus,
            LastOperationSummary = _lastOperationSummary
        };
        set
        {
            _tagDeviceId = value.DeviceId;
            _createdDateUtc = value.CreatedDateUtc;
            _lastUpdatedDateUtc = value.LastUpdatedDateUtc;
            _lastKnownStatus = value.LastKnownStatus;
            _lastOperationSummary = value.LastOperationSummary;
            _nameTextBox.Text = value.Name;
            _ipAddressTextBox.Text = value.IpAddress;
            _macAddressTextBox.Text = value.MacAddress;
            _broadcastAddressTextBox.Text = value.BroadcastAddress;
            _wolPortNumeric.Value = value.WolPort;
            _sshHostTextBox.Text = value.SshHost;
            _sshPortNumeric.Value = value.SshPort;
            _sshUsernameTextBox.Text = value.SshUsername;
            _sshPasswordTextBox.Text = value.SshPassword;
            _operatingSystemComboBox.SelectedItem = value.OperatingSystemType;
            _autoStartCheckBox.Checked = value.AutoStartEnabled;
            _autoShutdownCheckBox.Checked = value.AutoShutdownEnabled;
            _manualControlCheckBox.Checked = value.ManualControlEnabled;
            _activeCheckBox.Checked = value.IsActive;
            _timeoutSecondsNumeric.Value = value.TimeoutSeconds;
            _retryCountNumeric.Value = value.RetryCount;
            _descriptionTextBox.Text = value.Description;
        }
    }

    public string ViewTitle { set => Text = value; }

    private Guid _tagDeviceId;
    private DateTime _createdDateUtc = DateTime.UtcNow;
    private DateTime _lastUpdatedDateUtc = DateTime.UtcNow;
    private DevicePowerStatus _lastKnownStatus = DevicePowerStatus.Unknown;
    private string _lastOperationSummary = "No operations yet";

    public DialogResult ShowDialogView(IWin32Window owner) => ShowDialog(owner);

    public void CloseView(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    public void ShowWarning(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void BuildLayout()
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            AutoScroll = true
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(container, "Device Name", _nameTextBox);
        AddRow(container, "IP Address", _ipAddressTextBox);
        AddRow(container, "MAC Address", _macAddressTextBox);
        AddRow(container, "Broadcast Address", _broadcastAddressTextBox);
        AddRow(container, "WOL Port", _wolPortNumeric);
        AddRow(container, "SSH Host", _sshHostTextBox);
        AddRow(container, "SSH Port", _sshPortNumeric);
        AddRow(container, "SSH Username", _sshUsernameTextBox);
        AddRow(container, "SSH Password", _sshPasswordTextBox);
        AddRow(container, "Operating System", _operatingSystemComboBox);
        AddRow(container, "Timeout Seconds", _timeoutSecondsNumeric);
        AddRow(container, "Retry Count", _retryCountNumeric);
        AddRow(container, "Description", _descriptionTextBox);
        container.Controls.Add(_autoStartCheckBox, 1, container.RowCount++);
        container.Controls.Add(_autoShutdownCheckBox, 1, container.RowCount++);
        container.Controls.Add(_manualControlCheckBox, 1, container.RowCount++);
        container.Controls.Add(_activeCheckBox, 1, container.RowCount++);

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
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        Controls.Add(container);
        Controls.Add(buttonsPanel);
    }

    private static void AddRow(TableLayoutPanel table, string labelText, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 8, 8, 0)
        }, 0, table.RowCount);
        control.Dock = DockStyle.Top;
        table.Controls.Add(control, 1, table.RowCount);
        table.RowCount++;
    }
}
