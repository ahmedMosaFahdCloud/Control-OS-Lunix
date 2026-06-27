using Control_OS_Lunix.Model;
using Control_OS_Lunix.Service;

namespace Control_OS_Lunix;

public sealed class DeviceDialogForm : Form
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

    private readonly DevicePowerConfig _originalDevice;

    public DeviceDialogForm(GlobalSettings settings, DevicePowerConfig? existingDevice = null)
    {
        _originalDevice = existingDevice?.Clone() ?? new DevicePowerConfig
        {
            BroadcastAddress = settings.DefaultBroadcastAddress,
            WolPort = settings.DefaultWolPort
        };

        Text = existingDevice is null ? "Add Device" : "Edit Device";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 560;
        Height = 700;

        _operatingSystemComboBox.DataSource = Enum.GetValues<DeviceOperatingSystemType>();

        BuildLayout();
        LoadValues();
    }

    public DevicePowerConfig? Result { get; private set; }

    private void BuildLayout()
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 0,
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
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };

        saveButton.Click += SaveButton_Click;
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        Controls.Add(container);
        Controls.Add(buttonsPanel);
    }

    private void LoadValues()
    {
        _nameTextBox.Text = _originalDevice.Name;
        _ipAddressTextBox.Text = _originalDevice.IpAddress;
        _macAddressTextBox.Text = _originalDevice.MacAddress;
        _broadcastAddressTextBox.Text = _originalDevice.BroadcastAddress;
        _wolPortNumeric.Value = _originalDevice.WolPort;
        _sshHostTextBox.Text = _originalDevice.SshHost;
        _sshPortNumeric.Value = _originalDevice.SshPort;
        _sshUsernameTextBox.Text = _originalDevice.SshUsername;
        _sshPasswordTextBox.Text = _originalDevice.SshPassword;
        _operatingSystemComboBox.SelectedItem = _originalDevice.OperatingSystemType;
        _autoStartCheckBox.Checked = _originalDevice.AutoStartEnabled;
        _autoShutdownCheckBox.Checked = _originalDevice.AutoShutdownEnabled;
        _manualControlCheckBox.Checked = _originalDevice.ManualControlEnabled;
        _activeCheckBox.Checked = _originalDevice.IsActive;
        _timeoutSecondsNumeric.Value = _originalDevice.TimeoutSeconds;
        _retryCountNumeric.Value = _originalDevice.RetryCount;
        _descriptionTextBox.Text = _originalDevice.Description;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        DevicePowerConfig device = _originalDevice.Clone();
        device.Name = _nameTextBox.Text.Trim();
        device.IpAddress = _ipAddressTextBox.Text.Trim();
        device.MacAddress = _macAddressTextBox.Text.Trim();
        device.BroadcastAddress = _broadcastAddressTextBox.Text.Trim();
        device.WolPort = (int)_wolPortNumeric.Value;
        device.SshHost = _sshHostTextBox.Text.Trim();
        device.SshPort = (int)_sshPortNumeric.Value;
        device.SshUsername = _sshUsernameTextBox.Text.Trim();
        device.SshPassword = _sshPasswordTextBox.Text;
        device.OperatingSystemType = (DeviceOperatingSystemType)_operatingSystemComboBox.SelectedItem!;
        device.AutoStartEnabled = _autoStartCheckBox.Checked;
        device.AutoShutdownEnabled = _autoShutdownCheckBox.Checked;
        device.ManualControlEnabled = _manualControlCheckBox.Checked;
        device.IsActive = _activeCheckBox.Checked;
        device.TimeoutSeconds = (int)_timeoutSecondsNumeric.Value;
        device.RetryCount = (int)_retryCountNumeric.Value;
        device.Description = _descriptionTextBox.Text.Trim();
        device.LastUpdatedDateUtc = DateTime.UtcNow;

        string? validationMessage = DeviceValidator.ValidateForSave(device);
        if (validationMessage is not null)
        {
            MessageBox.Show(this, validationMessage, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Result = device;
        DialogResult = DialogResult.OK;
        Close();
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
